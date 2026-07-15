using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Goals;

// Phase 4.4: the live view of an accepted portfolio — marked to market from the
// registry, with target vs actual weights and the next review date.
public sealed class GoalPortfolioEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly string[] Etfs = ["SPY", "GLD", "AGG", "BIL"];

    private async Task SetPricesAsync(double price)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var set = db.Set<Instrument>();
        foreach (string symbol in Etfs)
        {
            set.FirstOrDefault(x => x.Symbol == symbol)?.UpdateStats(0.12, 5_000_000, price, null, DateTime.UtcNow);
        }
        await db.SaveChangesAsync();
    }

    private static object QuestionnaireBody() => new
    {
        goalId = (Guid?)null,
        goalType = "retirement",
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

    private async Task AcceptProposalAsync(Guid goalId)
    {
        Guid proposalId = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{proposalId}/accept", new { }))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_goal_without_an_accepted_proposal_has_no_portfolio_yet()
    {
        Guid goalId = await OnboardAsync("gp-none@quantwise.test");

        (await Client.GetAsync($"/api/goals/{goalId}/portfolio"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_accepted_portfolio_is_marked_to_market_at_its_entry_prices()
    {
        await SetPricesAsync(100.0);
        Guid goalId = await OnboardAsync("gp-live@quantwise.test");
        await AcceptProposalAsync(goalId);

        JsonElement body = await (await Client.GetAsync($"/api/goals/{goalId}/portfolio"))
            .Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("templateKey").GetString().Should().Be("retirement_set_and_forget");
        body.GetProperty("pricesComplete").GetBoolean().Should().BeTrue();
        body.GetProperty("nav").GetDouble().Should().BeApproximately(10_000, 1.0);
        body.GetProperty("totalReturnPct").GetDouble().Should().BeApproximately(0, 1e-6);
        body.GetProperty("drawdownPct").GetDouble().Should().BeApproximately(0, 1e-6);

        // Freshly accepted → actual weights sit exactly on target.
        foreach (JsonElement p in body.GetProperty("positions").EnumerateArray())
        {
            p.GetProperty("actualWeight").GetDouble()
                .Should().BeApproximately(p.GetProperty("targetWeight").GetDouble(), 1e-6);
            p.GetProperty("driftPct").GetDouble().Should().BeApproximately(0, 1e-6);
        }

        // Retirement rebalances semi-annually → next review ~6 months out.
        DateTime nextReview = body.GetProperty("nextReviewDate").GetDateTime();
        nextReview.Should().BeAfter(DateTime.UtcNow.AddMonths(5));
        body.GetProperty("rebalanceCadence").GetString().Should().Be("semi_annual");
    }

    [Fact]
    public async Task A_price_drop_shows_up_as_negative_return_and_drawdown()
    {
        await SetPricesAsync(100.0);
        Guid goalId = await OnboardAsync("gp-drop@quantwise.test");
        await AcceptProposalAsync(goalId);

        await SetPricesAsync(90.0); // −10% across the book

        JsonElement body = await (await Client.GetAsync($"/api/goals/{goalId}/portfolio"))
            .Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("nav").GetDouble().Should().BeApproximately(9_000, 1.0);
        body.GetProperty("totalReturnPct").GetDouble().Should().BeApproximately(-0.10, 1e-6);
        // High-water mark is the acceptance NAV → 10% below it.
        body.GetProperty("drawdownPct").GetDouble().Should().BeApproximately(0.10, 1e-6);
    }

    [Fact]
    public async Task A_position_running_up_shows_as_drift_from_target()
    {
        await SetPricesAsync(100.0);
        Guid goalId = await OnboardAsync("gp-drift@quantwise.test");
        await AcceptProposalAsync(goalId);

        // SPY (40% target) doubles → its actual weight climbs above target.
        using (IServiceScope scope = Factory.Services.CreateScope())
        {
            PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            db.Set<Instrument>().First(x => x.Symbol == "SPY").UpdateStats(0.12, 5_000_000, 200.0, null, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        JsonElement body = await (await Client.GetAsync($"/api/goals/{goalId}/portfolio"))
            .Content.ReadFromJsonAsync<JsonElement>();

        JsonElement spy = body.GetProperty("positions").EnumerateArray()
            .Single(p => p.GetProperty("symbol").GetString() == "SPY");
        spy.GetProperty("actualWeight").GetDouble().Should().BeGreaterThan(spy.GetProperty("targetWeight").GetDouble());
        spy.GetProperty("driftPct").GetDouble().Should().BeGreaterThan(0.10);
        body.GetProperty("totalReturnPct").GetDouble().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Another_user_cannot_read_a_goals_portfolio()
    {
        await SetPricesAsync(100.0);
        Guid goalId = await OnboardAsync("gp-owner@quantwise.test");
        await AcceptProposalAsync(goalId);

        (_, string intruder) = await RegisterAndLoginAsync("gp-intruder@quantwise.test");
        Authorize(intruder);

        (await Client.GetAsync($"/api/goals/{goalId}/portfolio"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
