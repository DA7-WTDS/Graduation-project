using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Recommendations;

// Covers FR-14/FR-15: the secured daily-run ingest, UTC-safe persistence on real
// Postgres, and idempotency on the generation timestamp.
public sealed class IngestEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Ingest_with_the_valid_key_persists_the_run()
    {
        HttpResponseMessage response = await IngestAsync(DateTime.UtcNow, IntegrationTestWebAppFactory.IngestApiKey);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using IServiceScope scope = Factory.Services.CreateScope();
        RecommendationsDbContext db = scope.ServiceProvider.GetRequiredService<RecommendationsDbContext>();
        db.Set<DailyRun>().Count().Should().Be(1);
        db.Set<StockPrediction>().Count().Should().Be(1);
    }

    [Fact]
    public async Task Ingest_with_a_wrong_key_is_unauthorized()
    {
        HttpResponseMessage response = await IngestAsync(DateTime.UtcNow, "not-the-key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ingesting_the_same_timestamp_twice_is_idempotent()
    {
        DateTime generatedAt = new(2026, 6, 17, 1, 0, 0, DateTimeKind.Utc);

        HttpResponseMessage first = await IngestAsync(generatedAt, IntegrationTestWebAppFactory.IngestApiKey);
        HttpResponseMessage second = await IngestAsync(generatedAt, IntegrationTestWebAppFactory.IngestApiKey);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("runId").GetGuid();
        Guid secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("runId").GetGuid();
        secondId.Should().Be(firstId);

        using IServiceScope scope = Factory.Services.CreateScope();
        RecommendationsDbContext db = scope.ServiceProvider.GetRequiredService<RecommendationsDbContext>();
        db.Set<DailyRun>().Count().Should().Be(1);
    }

    private async Task<HttpResponseMessage> IngestAsync(DateTime generatedAt, string key)
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

        var payload = new { generated_at = generatedAt, count = 1, records = new[] { record } };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/daily-results")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Pipeline-Key", key);

        return await Client.SendAsync(request);
    }
}
