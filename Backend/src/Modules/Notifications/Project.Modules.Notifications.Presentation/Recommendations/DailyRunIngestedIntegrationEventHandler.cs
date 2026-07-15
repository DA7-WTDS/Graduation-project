using System.Globalization;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.IntegrationEvents;
using Project.Modules.Users.PublicApi;

namespace Project.Modules.Notifications.Presentation.Recommendations;

/// <summary>
/// Fans out a personalized "new recommendations are ready" notification to every
/// user with a portfolio whenever the Recommendations module ingests a fresh
/// daily run from the pipeline.
/// </summary>
public sealed class DailyRunIngestedIntegrationEventHandler(
    ISender sender,
    IPortfolioApi portfolioApi,
    IUsersApi usersApi,
    ILogger<DailyRunIngestedIntegrationEventHandler> logger)
    : IntegrationEventHandler<DailyRunIngestedIntegrationEvent>
{
    public override async Task Handle(DailyRunIngestedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> userIds = await portfolioApi.GetProfiledUserIdsAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            logger.LogInformation("DailyRunIngested {DailyRunId}: no profiled users to notify.", integrationEvent.DailyRunId);
            return;
        }

        string runDate = integrationEvent.GeneratedAt.ToString("MMMM d", CultureInfo.InvariantCulture);
        int notified = 0;

        foreach (Guid userId in userIds)
        {
            try
            {
                MonitoringProfileResponse? profile = await portfolioApi.GetMonitoringProfileAsync(userId, cancellationToken);
                if (profile is null)
                {
                    continue;
                }

                UserResponse? user = await usersApi.GetAsync(userId, cancellationToken);
                string firstName = string.IsNullOrWhiteSpace(user?.FirstName) ? "there" : user!.FirstName;

                string message =
                    $"Hi {firstName}, your fresh {profile.RiskProfile}-tuned picks for {runDate} are ready. " +
                    "Open your dashboard to see what to buy, hold, or sell today.";

                Result<Guid> result = await sender.Send(new CreateNotificationCommand(
                    userId,
                    "New recommendations are ready",
                    message,
                    NotificationType.Info), cancellationToken);

                if (result.IsFailed)
                {
                    logger.LogError("Failed to create recommendations notification for user {UserId}: {Error}",
                        userId, result.Errors[0].Message);
                }
                else
                {
                    notified++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception creating recommendations notification for user {UserId}", userId);
            }
        }

        logger.LogInformation("DailyRunIngested {DailyRunId}: notified {Count}/{Total} users.",
            integrationEvent.DailyRunId, notified, userIds.Count);
    }
}
