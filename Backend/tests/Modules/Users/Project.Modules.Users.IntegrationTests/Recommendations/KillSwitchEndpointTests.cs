using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Recommendations;

// Covers § 6.2: the kill switch end to end. Only Published runs are served;
// quarantined runs are persisted but invisible; an operator can roll back a
// published run and undo it, and invalid transitions are rejected.
public sealed class KillSwitchEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Rolling_back_a_published_run_hides_it_from_serving_until_republished()
    {
        (_, string token) = await RegisterAndLoginAsync("killswitch@quantwise.test");

        // Ingest a clean run — lands Published (manual approval off in tests).
        Guid runId = await IngestRunAsync(DateTime.UtcNow, status: "ok");

        Authorize(token);
        (await Client.GetAsync("/api/predictions")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Operator pulls the run.
        HttpResponseMessage rollback = await FlipStatusAsync(runId, "rolled_back", "bad vendor data spotted after publish");
        rollback.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rollback.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString()
            .Should().Be("RolledBack");

        // The one WHERE clause at work: nothing published, nothing served.
        Authorize(token);
        (await Client.GetAsync("/api/predictions")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Undo.
        (await FlipStatusAsync(runId, "published", "false alarm")).StatusCode.Should().Be(HttpStatusCode.OK);
        Authorize(token);
        (await Client.GetAsync("/api/predictions")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_quarantined_run_is_persisted_but_never_served()
    {
        (_, string token) = await RegisterAndLoginAsync("quarantine@quantwise.test");

        Guid runId = await IngestRunAsync(
            DateTime.UtcNow, status: "quarantined",
            gateFailures: ["coverage: 40/100 tickers scored (40%, min 60%)"]);

        // Persisted for audit, visible to the operator...
        HttpResponseMessage list = await ListRunsAsync();
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement runs = await list.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement run = runs.EnumerateArray().Single(r => r.GetProperty("runId").GetGuid() == runId);
        run.GetProperty("status").GetString().Should().Be("Quarantined");
        run.GetProperty("statusReason").GetString().Should().Contain("coverage");

        // ...but invisible to users.
        Authorize(token);
        (await Client.GetAsync("/api/predictions")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_transitions_are_rejected()
    {
        Guid runId = await IngestRunAsync(DateTime.UtcNow, status: "ok"); // lands Published

        // Published → Quarantined is not a legal move (roll back instead).
        HttpResponseMessage response = await FlipStatusAsync(runId, "quarantined", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Unknown status strings are rejected outright.
        (await FlipStatusAsync(runId, "yeeted", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Kill_switch_endpoints_require_the_pipeline_key()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/daily-runs");
        request.Headers.Add("X-Pipeline-Key", "not-the-key");

        (await Client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> IngestRunAsync(DateTime generatedAt, string status, string[]? gateFailures = null)
    {
        var record = new PredictionRecordDto
        {
            Ticker = "AAPL",
            Direction = "UP",
            ChangePct = 2.5,
            Confidence = 0.9,
            SentimentScore = 0.4,
            Signal = "POSITIVE",
            Agreement = "CONFIRMED",
            RiskLevel = "LOW",
            ConvictionScore = 0.85,
            RiskFlags = ["signal_confirmed"],
            Rationale = "UP +2.5% | sentiment POSITIVE | confirmed | no flags",
        };

        var payload = new
        {
            generated_at = generatedAt,
            count = 1,
            records = new[] { record },
            status,
            gate_failures = gateFailures ?? [],
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/daily-results")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);

        HttpResponseMessage response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("runId").GetGuid();
    }

    private async Task<HttpResponseMessage> FlipStatusAsync(Guid runId, string status, string? reason)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/internal/daily-runs/{runId}/status")
        {
            Content = JsonContent.Create(new { status, reason }),
        };
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);
        return await Client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ListRunsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/daily-runs");
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);
        return await Client.SendAsync(request);
    }
}
