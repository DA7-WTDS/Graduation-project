using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Notifications.Infrastructure.Database;
using Project.Modules.Notifications.Presentation.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Portfolios;
using Project.Modules.Portfolio.IntegrationEvents;
using Project.Modules.Users.IntegrationTests.Infrastructure;
using Quartz;

namespace Project.Modules.Users.IntegrationTests.Monitoring;

// § 3.5 digest: paced by the engagement answer. Inception is backdated so the
// cadence has "elapsed" without waiting a month.
public sealed class PortfolioDigestTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly string[] Etfs = ["SPY", "GLD", "AGG", "BIL"];

    private async Task SetPricesAsync(double price)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        foreach (string symbol in Etfs)
        {
            db.Set<Instrument>().FirstOrDefault(x => x.Symbol == symbol)
                ?.UpdateStats(0.12, 5_000_000, price, null, DateTime.UtcNow);
        }
        await db.SaveChangesAsync();
    }

    private static object QuestionnaireBody(string engagement) => new
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
        engagement,
        usdComfort = "comfortable",
        affordLossConfirmed = false,
    };

    private async Task<(Guid UserId, Guid GoalId)> OnboardAndAcceptAsync(string email, string engagement)
    {
        (Guid userId, string token) = await RegisterAndLoginAsync(email);
        Authorize(token);
        Guid goalId = (await (await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody(engagement)))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("goalId").GetGuid();
        Guid proposalId = (await (await Client.PostAsJsonAsync($"/api/goals/{goalId}/proposals", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await Client.PostAsJsonAsync($"/api/portfolio-proposals/{proposalId}/accept", new { })).EnsureSuccessStatusCode();
        return (userId, goalId);
    }

    /// <summary>Backdates inception so the cadence has elapsed (inception is set
    /// by the aggregate at accept time and is otherwise immutable).</summary>
    private async Task BackdateInceptionAsync(Guid goalId, int days)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""Portfolio"".goal_portfolios SET inception_date = {DateTime.UtcNow.AddDays(-days)} WHERE goal_id = {goalId}");
    }

    private async Task RunDigestJobAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        var job = new PortfolioDigestJob(
            scope.ServiceProvider.GetRequiredService<IGoalPortfolioRepository>(),
            scope.ServiceProvider.GetRequiredService<IGoalRepository>(),
            scope.ServiceProvider.GetRequiredService<IPortfolioProposalRepository>(),
            scope.ServiceProvider.GetRequiredService<IEventBus>(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            NullLogger<PortfolioDigestJob>.Instance);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        await job.Execute(context);
    }

    private DateTime? LastDigestAt(Guid goalId)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        return db.Set<GoalPortfolio>().Single(p => p.GoalId == goalId && p.Status == GoalPortfolioStatus.Active).LastDigestAt;
    }

    [Fact]
    public async Task A_fresh_portfolio_gets_no_digest()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("dg-fresh@quantwise.test", "monthly");

        await RunDigestJobAsync();

        LastDigestAt(goalId).Should().BeNull();
    }

    [Fact]
    public async Task A_monthly_investor_gets_a_digest_once_the_month_elapses()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("dg-monthly@quantwise.test", "monthly");
        await BackdateInceptionAsync(goalId, 35);

        await RunDigestJobAsync();
        DateTime? first = LastDigestAt(goalId);
        first.Should().NotBeNull();

        // Running again the same day must not re-send — the clock re-armed.
        await RunDigestJobAsync();
        LastDigestAt(goalId).Should().Be(first);
    }

    [Fact]
    public async Task A_set_and_forget_investor_is_not_mailed_monthly()
    {
        await SetPricesAsync(100.0);
        (_, Guid goalId) = await OnboardAndAcceptAsync("dg-saf@quantwise.test", "set_and_forget");

        // Two months in: a monthly investor would be overdue; this one is not.
        await BackdateInceptionAsync(goalId, 60);
        await RunDigestJobAsync();
        LastDigestAt(goalId).Should().BeNull();

        // A full quarter in, the check-in goes out.
        await BackdateInceptionAsync(goalId, 95);
        await RunDigestJobAsync();
        LastDigestAt(goalId).Should().NotBeNull();
    }

    [Fact]
    public async Task The_digest_notification_reports_value_and_next_review()
    {
        (Guid userId, _) = await OnboardAndAcceptAsync("dg-note@quantwise.test", "set_and_forget");

        using (IServiceScope scope = Factory.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PortfolioDigestDueIntegrationEventHandler>();
            await handler.Handle(new PortfolioDigestDueIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, userId, Guid.NewGuid(),
                "Retirement / Set-and-Forget", "SetAndForget", 90,
                10_500, 0.05, 0, DateTime.UtcNow.AddMonths(6)));
        }

        using IServiceScope check = Factory.Services.CreateScope();
        NotificationsDbContext db = check.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        Notification note = db.Set<Notification>().Single(n => n.UserId == userId);

        note.Title.Should().StartWith("Quarterly check-in");
        note.Message.Should().Contain("$10,500").And.Contain("up +5.0%");
        note.Message.Should().Contain("Nothing needs doing");
    }
}
