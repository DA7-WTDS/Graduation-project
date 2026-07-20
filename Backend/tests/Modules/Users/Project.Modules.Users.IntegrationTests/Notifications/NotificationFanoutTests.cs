using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Notifications.Infrastructure.Database;
using Project.Modules.Notifications.Presentation.Recommendations;
using Project.Modules.Recommendations.IntegrationEvents;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Notifications;

// Covers FR-18: when a daily run is published (§ 6.2 — never on mere ingestion),
// every onboarded user gets a personalised "new recommendations are ready"
// notification. The integration event is consumed into the inbox and the fan-out
// runs from a background job, so this test drives the real fan-out handler
// directly against the containerised database (cross-module:
// Recommendations event -> Portfolio + Users public APIs -> Notifications persistence).
public sealed class NotificationFanoutTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task A_published_daily_run_fans_out_one_notification_per_onboarded_user()
    {
        // Two users, each with a scored goal.
        (_, string tokenA) = await RegisterAndLoginAsync("fan-a@quantwise.test");
        Authorize(tokenA);
        await OnboardAsync();

        (_, string tokenB) = await RegisterAndLoginAsync("fan-b@quantwise.test");
        Authorize(tokenB);
        await OnboardAsync();

        // Run the real fan-out handler for a published daily run.
        using (IServiceScope scope = Factory.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<DailyRunPublishedIntegrationEventHandler>();
            var @event = new DailyRunPublishedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow);

            await handler.Handle(@event, CancellationToken.None);
        }

        // One notification per onboarded user was persisted.
        using (IServiceScope scope = Factory.Services.CreateScope())
        {
            NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            db.Set<Notification>().Count().Should().Be(2);
        }

        // ...and the recipient can read theirs through the notifications endpoint (FR-19).
        Authorize(tokenA);
        HttpResponseMessage mine = await Client.GetAsync("/api/notifications");
        mine.EnsureSuccessStatusCode();
        JsonElement body = await mine.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }
}
