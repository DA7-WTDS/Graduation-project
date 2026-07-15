using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Goals;

// Phase 2 questionnaire flow: raw answers in, versioned server-scored profile out.
// Answers and profiles are append-only — this is the FRA suitability record.
public sealed class GoalEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static object QuestionnaireBody(
        Guid? goalId = null,
        string goalType = "retirement",
        bool hasEmergencyFund = true,
        string marketReaction = "buy_more",
        string experience = "experienced") => new
        {
            goalId,
            goalType,
            horizonYears = 10,
            investmentAmount = 10000m,
            monthlyContribution = 500m,
            hasEmergencyFund,
            incomeStability = "stable",
            savingsShare = "less_than_ten_percent",
            marketReaction,
            experience,
            engagement = "set_and_forget",
            usdComfort = "comfortable",
            affordLossConfirmed = false,
        };

    [Fact]
    public async Task Submitting_the_questionnaire_returns_a_server_scored_profile()
    {
        (_, string token) = await RegisterAndLoginAsync("goal-owner@quantwise.test");
        Authorize(token);

        HttpResponseMessage submit = await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody());
        submit.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await submit.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("profileVersion").GetInt32().Should().Be(1);
        body.GetProperty("scoringVersion").GetString().Should().Be("v1");
        body.GetProperty("riskBand").GetString().Should().Be("Aggressive");
        body.GetProperty("effectiveRisk").GetInt32().Should().Be(100);
        body.GetProperty("speculativeUnlocked").GetBoolean().Should().BeFalse();
        // The amount is a fact about the goal now — the optimizer sizes against it.
        body.GetProperty("investmentAmount").GetDecimal().Should().Be(10000m);
    }

    [Fact]
    public async Task Capacity_gates_override_a_risk_hungry_temperament()
    {
        (_, string token) = await RegisterAndLoginAsync("gated@quantwise.test");
        Authorize(token);

        // Wants maximum risk, but has no emergency fund — capacity wins.
        HttpResponseMessage submit = await Client.PostAsJsonAsync(
            "/api/goals/questionnaire", QuestionnaireBody(hasEmergencyFund: false));

        JsonElement body = await submit.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("riskBand").GetString().Should().Be("Conservative");
        body.GetProperty("tolerance").GetInt32().Should().Be(100);
        body.GetProperty("capacity").GetInt32().Should().Be(35);
    }

    [Fact]
    public async Task Retake_appends_a_new_profile_version_and_keeps_every_response()
    {
        (_, string token) = await RegisterAndLoginAsync("goal-retake@quantwise.test");
        Authorize(token);

        HttpResponseMessage first = await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody());
        Guid goalId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("goalId").GetGuid();

        HttpResponseMessage retake = await Client.PostAsJsonAsync(
            "/api/goals/questionnaire",
            QuestionnaireBody(goalId: goalId, marketReaction: "sell_some", experience: "beginner"));
        retake.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await retake.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("goalId").GetGuid().Should().Be(goalId);
        body.GetProperty("profileVersion").GetInt32().Should().Be(2);
        body.GetProperty("riskBand").GetString().Should().Be("Conservative");

        // Append-only storage: both responses and both profile versions survive,
        // and the retake updates the single goal in place rather than forking it.
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        db.Set<Project.Modules.Portfolio.Domain.Goals.QuestionnaireResponse>()
            .Count(q => q.GoalId == goalId).Should().Be(2);
        db.Set<Project.Modules.Portfolio.Domain.Goals.InvestorProfile>()
            .Count(p => p.GoalId == goalId).Should().Be(2);
        db.Set<Project.Modules.Portfolio.Domain.Goals.Goal>()
            .Count(g => g.Id == goalId).Should().Be(1);
    }

    [Fact]
    public async Task Get_goals_lists_the_goal_with_its_latest_profile()
    {
        (_, string token) = await RegisterAndLoginAsync("goal-list@quantwise.test");
        Authorize(token);
        (await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody())).EnsureSuccessStatusCode();

        HttpResponseMessage list = await Client.GetAsync("/api/goals");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await list.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(1);
        body[0].GetProperty("type").GetString().Should().Be("Retirement");
        body[0].GetProperty("profile").GetProperty("riskBand").GetString().Should().Be("Aggressive");
    }

    [Fact]
    public async Task A_user_cannot_retake_someone_elses_goal()
    {
        (_, string tokenA) = await RegisterAndLoginAsync("goal-a@quantwise.test");
        Authorize(tokenA);
        HttpResponseMessage first = await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody());
        Guid goalId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("goalId").GetGuid();

        (_, string tokenB) = await RegisterAndLoginAsync("goal-b@quantwise.test");
        Authorize(tokenB);
        HttpResponseMessage hijack = await Client.PostAsJsonAsync(
            "/api/goals/questionnaire", QuestionnaireBody(goalId: goalId));

        hijack.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_answer_tokens_are_rejected()
    {
        (_, string token) = await RegisterAndLoginAsync("goal-bad@quantwise.test");
        Authorize(token);

        HttpResponseMessage submit = await Client.PostAsJsonAsync(
            "/api/goals/questionnaire", QuestionnaireBody(goalType: "yolo"));

        submit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submitting_without_a_token_is_unauthorized()
    {
        HttpResponseMessage submit = await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody());
        submit.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
