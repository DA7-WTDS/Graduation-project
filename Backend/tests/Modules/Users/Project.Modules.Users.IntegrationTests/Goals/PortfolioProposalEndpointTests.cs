using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Goals;

// Phase 4.1: proposals are immutable, versioned, and one-accepted-at-a-time.
// Accepting a newer version supersedes the prior accepted one; the persisted
// proposal reproduces the ephemeral draft exactly (same InputsHash).
public sealed class PortfolioProposalEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
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

    private async Task<Guid> OnboardAsync(string email)
    {
        (_, string token) = await RegisterAndLoginAsync(email);
        Authorize(token);
        JsonElement submitted = await (await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody()))
            .Content.ReadFromJsonAsync<JsonElement>();
        return submitted.GetProperty("goalId").GetGuid();
    }

    [Fact]
    public async Task Creating_a_proposal_persists_an_immutable_first_version()
    {
        Guid goalId = await OnboardAsync("prop-create@quantwise.test");

        HttpResponseMessage create = await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        JsonElement body = await create.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("version").GetInt32().Should().Be(1);
        body.GetProperty("status").GetString().Should().Be("Proposed");
        body.GetProperty("templateKey").GetString().Should().Be("retirement_set_and_forget");
        body.GetProperty("positions").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("inputsHash").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task A_persisted_proposal_reproduces_the_draft_exactly()
    {
        Guid goalId = await OnboardAsync("prop-determinism@quantwise.test");

        JsonElement draft = await (await Client.GetAsync($"/api/goals/{goalId}/portfolio-draft"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonElement proposal = await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();

        // The draft is a preview of the very computation the proposal freezes.
        proposal.GetProperty("inputsHash").GetString()
            .Should().Be(draft.GetProperty("inputsHash").GetString());
        proposal.GetProperty("positions").GetArrayLength()
            .Should().Be(draft.GetProperty("positions").GetArrayLength());
    }

    [Fact]
    public async Task Accepting_a_newer_proposal_supersedes_the_previously_accepted_one()
    {
        Guid goalId = await OnboardAsync("prop-supersede@quantwise.test");

        Guid v1 = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{v1}/accept", new { }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        Guid v2 = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        JsonElement acceptedV2 = await (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{v2}/accept", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        acceptedV2.GetProperty("status").GetString().Should().Be("Accepted");

        // List shows exactly one accepted (v2) and the older one superseded.
        JsonElement list = await (await Client.GetAsync($"/api/goals/{goalId}/proposals"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var byVersion = list.EnumerateArray()
            .ToDictionary(p => p.GetProperty("version").GetInt32(), p => p.GetProperty("status").GetString());
        byVersion[2].Should().Be("Accepted");
        byVersion[1].Should().Be("Superseded");
    }

    [Fact]
    public async Task A_superseded_proposal_cannot_be_re_accepted()
    {
        Guid goalId = await OnboardAsync("prop-resurrect@quantwise.test");

        Guid v1 = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Client.PostAsJsonAsync($"/api/portfolio-proposals/{v1}/accept", new { });

        Guid v2 = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Client.PostAsJsonAsync($"/api/portfolio-proposals/{v2}/accept", new { });

        // v1 is now superseded — accepting it again must 409.
        (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{v1}/accept", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Another_user_cannot_touch_a_goals_proposals()
    {
        Guid goalId = await OnboardAsync("prop-owner@quantwise.test");
        Guid proposalId = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (_, string intruderToken) = await RegisterAndLoginAsync("prop-intruder@quantwise.test");
        Authorize(intruderToken);

        (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetAsync($"/api/goals/{goalId}/proposals"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{proposalId}/accept", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
