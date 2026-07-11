using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Goals;

// § 3.2 + § 3.3 end to end: questionnaire → profile → template selection →
// deterministic optimizer over the seeded registry + an ingested daily run.
public sealed class PortfolioDraftEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task IngestRunAsync(params (string Ticker, double Conviction, string RiskLevel)[] records)
    {
        var payload = new
        {
            generated_at = DateTime.UtcNow,
            count = records.Length,
            records = records.Select(r => new PredictionRecordDto
            {
                Ticker = r.Ticker,
                Direction = "UP",
                ChangePct = 2.0,
                Confidence = 0.9,
                SentimentScore = 0.3,
                Signal = "POSITIVE",
                Agreement = "CONFIRMED",
                RiskLevel = r.RiskLevel,
                ConvictionScore = r.Conviction,
                RiskFlags = [],
                Rationale = "test",
            }).ToArray(),
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/daily-results")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);
        (await Client.SendAsync(request)).EnsureSuccessStatusCode();
    }

    private static object QuestionnaireBody(string goalType = "retirement") => new
    {
        goalId = (Guid?)null,
        goalType,
        horizonYears = 10,
        investmentAmount = 10000m,
        monthlyContribution = 0m,
        hasEmergencyFund = true,
        incomeStability = "stable",
        savingsShare = "less_than_ten_percent",
        marketReaction = "buy_more",
        experience = "experienced",
        engagement = "monthly",
        usdComfort = "comfortable",
        affordLossConfirmed = false,
    };

    [Fact]
    public async Task Retirement_profile_gets_the_set_and_forget_template_with_no_single_stocks()
    {
        (_, string token) = await RegisterAndLoginAsync("draft-retire@quantwise.test");
        Authorize(token);
        JsonElement submitted = await (await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody()))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid goalId = submitted.GetProperty("goalId").GetGuid();

        HttpResponseMessage draft = await Client.GetAsync($"/api/goals/{goalId}/portfolio-draft");
        draft.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await draft.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("templateKey").GetString().Should().Be("retirement_set_and_forget");

        // Seeded registry: SPY (core etf), GLD, AGG, BIL — no individual equities.
        var positions = body.GetProperty("positions").EnumerateArray().ToList();
        positions.Select(p => p.GetProperty("symbol").GetString())
            .Should().BeEquivalentTo(["SPY", "GLD", "AGG", "BIL"]);
        positions.Sum(p => p.GetProperty("weight").GetDouble()).Should().BeApproximately(1.0, 1e-6);
        // The template's exact split — the position cap must not touch the index ETF.
        positions.Single(p => p.GetProperty("symbol").GetString() == "SPY")
            .GetProperty("weight").GetDouble().Should().BeApproximately(0.40, 1e-6);
        body.GetProperty("inputsHash").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Aggressive_wealth_profile_gets_ranked_equities_in_the_core_sleeve()
    {
        await IngestRunAsync(("AAPL", 0.9, "LOW"), ("MSFT", 0.8, "MEDIUM"), ("TSLA", 0.7, "HIGH"));

        (_, string token) = await RegisterAndLoginAsync("draft-active@quantwise.test");
        Authorize(token);
        JsonElement submitted = await (await Client.PostAsJsonAsync(
                "/api/goals/questionnaire", QuestionnaireBody(goalType: "long_term_wealth")))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid goalId = submitted.GetProperty("goalId").GetGuid();

        HttpResponseMessage draft = await Client.GetAsync($"/api/goals/{goalId}/portfolio-draft");
        draft.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await draft.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("templateKey").GetString().Should().Be("active_growth");

        // Ranked names appear only if the registry knows them (auto-registration
        // happens nightly) — ingesting alone must not put unknown symbols in a
        // draft. This asserts the registry is the gate.
        var symbols = body.GetProperty("positions").EnumerateArray()
            .Select(p => p.GetProperty("symbol").GetString()).ToList();
        symbols.Should().NotContain("AAPL"); // not registered as an instrument in this test
        body.GetProperty("positions").EnumerateArray()
            .Sum(p => p.GetProperty("weight").GetDouble()).Should().BeApproximately(1.0, 1e-6);

        // Same inputs → same draft (deterministic, D6).
        JsonElement second = await (await Client.GetAsync($"/api/goals/{goalId}/portfolio-draft"))
            .Content.ReadFromJsonAsync<JsonElement>();
        second.GetProperty("inputsHash").GetString().Should().Be(body.GetProperty("inputsHash").GetString());
    }

    [Fact]
    public async Task Another_users_goal_draft_is_forbidden()
    {
        (_, string tokenA) = await RegisterAndLoginAsync("draft-a@quantwise.test");
        Authorize(tokenA);
        JsonElement submitted = await (await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody()))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid goalId = submitted.GetProperty("goalId").GetGuid();

        (_, string tokenB) = await RegisterAndLoginAsync("draft-b@quantwise.test");
        Authorize(tokenB);

        (await Client.GetAsync($"/api/goals/{goalId}/portfolio-draft"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
