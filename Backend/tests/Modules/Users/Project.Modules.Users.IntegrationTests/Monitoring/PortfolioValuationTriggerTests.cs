using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Notifications.Infrastructure.Database;
using Project.Modules.Notifications.Presentation.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.IntegrationEvents;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Portfolios;
using Project.Modules.Users.IntegrationTests.Infrastructure;
using Quartz;

namespace Project.Modules.Users.IntegrationTests.Monitoring;

// § 3.5 / 4.2: accepting a proposal opens a live portfolio; the nightly
// valuation job marks it to market and fires drawdown / drift on crossing.
// Instrument prices live in the Respawn-ignored registry, so every test sets
// the four sleeve-ETF prices up front for order-independence.
public sealed class PortfolioValuationTriggerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly string[] Etfs = ["SPY", "GLD", "AGG", "BIL"];

    private async Task SetPricesAsync(double price)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var set = db.Set<Instrument>();
        foreach (string symbol in Etfs)
        {
            Instrument? i = set.FirstOrDefault(x => x.Symbol == symbol);
            i?.UpdateStats(0.12, 5_000_000, price, null, DateTime.UtcNow);
        }
        await db.SaveChangesAsync();
    }

    private async Task SetPriceAsync(string symbol, double price)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        Instrument? i = db.Set<Instrument>().FirstOrDefault(x => x.Symbol == symbol);
        i?.UpdateStats(0.12, 5_000_000, price, null, DateTime.UtcNow);
        await db.SaveChangesAsync();
    }

    private static object QuestionnaireBody(string goalType, string engagement) => new
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
        engagement,
        usdComfort = "comfortable",
        affordLossConfirmed = false,
    };

    private async Task<(Guid UserId, Guid GoalId)> OnboardAndAcceptAsync(string email, string engagement = "monthly")
    {
        (Guid userId, string token) = await RegisterAndLoginAsync(email);
        Authorize(token);
        JsonElement submitted = await (await Client.PostAsJsonAsync(
                "/api/goals/questionnaire", QuestionnaireBody("retirement", engagement)))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid goalId = submitted.GetProperty("goalId").GetGuid();

        Guid proposalId = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{proposalId}/accept", new { }))
            .EnsureSuccessStatusCode();
        return (userId, goalId);
    }

    private async Task RunValuationJobAsync(double driftThreshold = 0.10)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        var job = new PortfolioValuationJob(
            scope.ServiceProvider.GetRequiredService<IGoalPortfolioRepository>(),
            scope.ServiceProvider.GetRequiredService<IInstrumentRepository>(),
            scope.ServiceProvider.GetRequiredService<IEventBus>(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Options.Create(new PortfolioValuationOptions { Market = "us", DriftThreshold = driftThreshold }),
            NullLogger<PortfolioValuationJob>.Instance);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        await job.Execute(context);
    }

    private GoalPortfolio LoadPortfolio(Guid goalId)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        return db.Set<GoalPortfolio>()
            .Include(p => p.Holdings)
            .Where(p => p.GoalId == goalId && p.Status == GoalPortfolioStatus.Active)
            .Single();
    }

    [Fact]
    public async Task Accepting_a_proposal_opens_a_live_portfolio_priced_at_acceptance()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("val-open@quantwise.test");

        GoalPortfolio p = LoadPortfolio(goalId);

        p.Holdings.Should().HaveCount(4);
        p.Holdings.Should().OnlyContain(h => h.Shares > 0 && h.EntryPrice == 100.0);
        p.HighWaterMarkNav.Should().BeApproximately(10_000, 1.0); // ≈ the invested amount
    }

    [Fact]
    public async Task A_broad_price_drop_fires_the_drawdown_alert()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("val-drawdown@quantwise.test");

        await SetPricesAsync(80.0); // −20%, past the retirement template's 15% threshold
        await RunValuationJobAsync();

        GoalPortfolio p = LoadPortfolio(goalId);
        p.DrawdownAlertActive.Should().BeTrue();
        p.LastNav.Should().BeApproximately(8_000, 1.0);
        p.HighWaterMarkNav.Should().BeApproximately(10_000, 1.0);
    }

    [Fact]
    public async Task A_shallow_dip_does_not_fire_the_drawdown_alert()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("val-shallow@quantwise.test");

        await SetPricesAsync(95.0); // −5%, within threshold
        await RunValuationJobAsync();

        LoadPortfolio(goalId).DrawdownAlertActive.Should().BeFalse();
    }

    [Fact]
    public async Task A_position_running_far_from_target_fires_the_drift_alert()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("val-drift@quantwise.test");

        // SPY (40% target) doubles → its weight jumps well past +10pp; a gain, so
        // it lifts the high-water mark (no drawdown) and isolates drift.
        await SetPriceAsync("SPY", 200.0);
        await RunValuationJobAsync();

        GoalPortfolio p = LoadPortfolio(goalId);
        p.DriftAlertActive.Should().BeTrue();
        p.DrawdownAlertActive.Should().BeFalse();
    }

    [Fact]
    public async Task Drawdown_notification_tone_follows_the_profile()
    {
        (Guid userId, _) = await OnboardAndAcceptAsync("val-tone@quantwise.test");

        using IServiceScope scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PortfolioDrawdownDetectedIntegrationEventHandler>();
        await handler.Handle(new PortfolioDrawdownDetectedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, userId, Guid.NewGuid(), 0.18, 0.15));

        using IServiceScope check = Factory.Services.CreateScope();
        NotificationsDbContext db = check.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        Notification note = db.Set<Notification>().Single(n => n.UserId == userId && n.Title.StartsWith("Portfolio down"));
        note.Message.Should().Contain("plan working"); // retirement → hold guidance
    }

    [Fact]
    public async Task Drift_notification_is_skipped_for_set_and_forget_investors()
    {
        (Guid userId, _) = await OnboardAndAcceptAsync("val-drift-skip@quantwise.test", engagement: "set_and_forget");

        using IServiceScope scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PortfolioDriftDetectedIntegrationEventHandler>();
        await handler.Handle(new PortfolioDriftDetectedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, userId, Guid.NewGuid(), 0.14, 0.10));

        using IServiceScope check = Factory.Services.CreateScope();
        NotificationsDbContext db = check.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        db.Set<Notification>().Count(n => n.UserId == userId).Should().Be(0);
    }
}
