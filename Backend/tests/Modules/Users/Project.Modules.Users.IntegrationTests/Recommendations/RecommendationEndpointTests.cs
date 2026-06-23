using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Recommendations;

// Covers FR-10 (personalised picks) and FR-12 (24h per-user cache short-circuits the
// second request, so the LLM is invoked at most once per user per day).
public sealed class RecommendationEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Returns_personalised_picks_and_serves_the_second_request_from_cache()
    {
        await IngestRunAsync();

        (_, string token) = await RegisterAndLoginAsync("rec@quantwise.test");
        Authorize(token);
        (await Client.PostAsJsonAsync("/api/portfolios", SamplePortfolioBody())).EnsureSuccessStatusCode();

        var fake = (FakeLlmClient)Factory.Services.GetRequiredService<ILlmClient>();
        int before = fake.Calls;

        HttpResponseMessage first = await Client.GetAsync("/api/recommendations");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await first.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("picks").GetArrayLength().Should().Be(2);

        int afterFirst = fake.Calls;
        afterFirst.Should().Be(before + 1, "the first request generates via the LLM");

        HttpResponseMessage second = await Client.GetAsync("/api/recommendations");
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        fake.Calls.Should().Be(afterFirst, "the second request is served from the 24-hour cache");
    }

    [Fact]
    public async Task Returns_an_error_when_no_daily_run_exists()
    {
        (_, string token) = await RegisterAndLoginAsync("norun@quantwise.test");
        Authorize(token);
        (await Client.PostAsJsonAsync("/api/portfolios", SamplePortfolioBody())).EnsureSuccessStatusCode();

        HttpResponseMessage response = await Client.GetAsync("/api/recommendations");

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    private async Task IngestRunAsync()
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

        var payload = new { generated_at = DateTime.UtcNow, count = 1, records = new[] { record } };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/daily-results")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);

        (await Client.SendAsync(request)).EnsureSuccessStatusCode();
    }
}
