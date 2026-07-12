using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Notifications.Infrastructure.Database;
using Project.Modules.Notifications.Presentation.Recommendations;
using Project.Modules.Recommendations.IntegrationEvents;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Monitoring;

// § 3.5 monitoring fan-out: same trigger, different message per profile — the
// retirement investor is talked off the ledge, the active investor is pointed
// at the opportunity; set-and-forget users are shielded from position noise.
public sealed class MonitoringTriggerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
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

    private async Task<Guid> OnboardAsync(string email, string goalType, string engagement)
    {
        (Guid userId, string token) = await RegisterAndLoginAsync(email);
        Authorize(token);
        (await Client.PostAsJsonAsync("/api/goals/questionnaire", QuestionnaireBody(goalType, engagement)))
            .EnsureSuccessStatusCode();
        return userId;
    }

    private List<Notification> NotificationsOf(Guid userId)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        return db.Set<Notification>().Where(n => n.UserId == userId).ToList();
    }

    [Fact]
    public async Task Market_crash_notifies_everyone_with_profile_matched_tone()
    {
        Guid retiree = await OnboardAsync("crash-retiree@quantwise.test", "retirement", "set_and_forget");
        Guid active = await OnboardAsync("crash-active@quantwise.test", "long_term_wealth", "daily");

        using (IServiceScope scope = Factory.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<MarketCrashDetectedIntegrationEventHandler>();
            await handler.Handle(new MarketCrashDetectedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, "^GSPC", -0.062, 5, DateTime.UtcNow.Date));
        }

        Notification retireeNote = NotificationsOf(retiree).Single(n => n.Title.StartsWith("Market alert"));
        Notification activeNote = NotificationsOf(active).Single(n => n.Title.StartsWith("Market alert"));

        // Same event, different coaching.
        retireeNote.Message.Should().Contain("staying the course");
        activeNote.Message.Should().Contain("tactical opportunities");
    }

    [Fact]
    public async Task Conviction_reversal_reaches_active_profiles_and_skips_set_and_forget()
    {
        Guid active = await OnboardAsync("rev-active@quantwise.test", "long_term_wealth", "monthly");
        Guid passive = await OnboardAsync("rev-passive@quantwise.test", "retirement", "set_and_forget");

        using (IServiceScope scope = Factory.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ConvictionReversalDetectedIntegrationEventHandler>();
            await handler.Handle(new ConvictionReversalDetectedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, active, ["AAPL", "TSLA"], DateTime.UtcNow));
            await handler.Handle(new ConvictionReversalDetectedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, passive, ["AAPL"], DateTime.UtcNow));
        }

        NotificationsOf(active).Should().ContainSingle(n => n.Title.Contains("AAPL, TSLA"));
        NotificationsOf(passive).Should().BeEmpty();
    }
}
