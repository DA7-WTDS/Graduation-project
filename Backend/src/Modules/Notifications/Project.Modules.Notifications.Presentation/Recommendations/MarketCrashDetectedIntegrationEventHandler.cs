using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.IntegrationEvents;

namespace Project.Modules.Notifications.Presentation.Recommendations;

/// <summary>
/// Market-crash fan-out (§ 3.5): everyone hears about it, but the tone follows
/// the profile — set-and-forget/conservative investors get "context + hold"
/// guidance (panic-selling a retirement plan is the one mistake that can't be
/// optimized away later), active risk-tolerant profiles get the opportunity
/// framing their template actually acts on.
/// </summary>
public sealed class MarketCrashDetectedIntegrationEventHandler(
    ISender sender,
    IPortfolioApi portfolioApi,
    ILogger<MarketCrashDetectedIntegrationEventHandler> logger)
    : IntegrationEventHandler<MarketCrashDetectedIntegrationEvent>
{
    public override async Task Handle(MarketCrashDetectedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> userIds = await portfolioApi.GetProfiledUserIdsAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            logger.LogInformation("MarketCrash {Index}: no profiled users to notify.", integrationEvent.IndexTicker);
            return;
        }

        string drop = integrationEvent.DropPct.ToString("P1");
        int notified = 0;

        foreach (Guid userId in userIds)
        {
            try
            {
                MonitoringProfileResponse? profile =
                    await portfolioApi.GetMonitoringProfileAsync(userId, cancellationToken);
                if (profile is null)
                {
                    continue;
                }

                bool holdGuidance =
                    string.Equals(profile.GoalType, "Retirement", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(profile.Engagement, "SetAndForget", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(profile.RiskProfile, "Conservative", StringComparison.OrdinalIgnoreCase);

                string message = holdGuidance
                    ? $"The market fell {drop} over the last {integrationEvent.WindowDays} trading days. " +
                      "Drops like this are normal on the way to long-term goals — your plan already assumes them. " +
                      "Selling now locks in the loss; staying the course is what the plan is built for."
                    : $"The market fell {drop} over the last {integrationEvent.WindowDays} trading days. " +
                      "Volatility like this is when disciplined plans earn their keep — check your dashboard: " +
                      "your risk limits are enforced automatically, and dips can surface tactical opportunities.";

                Result<Guid> result = await sender.Send(new CreateNotificationCommand(
                    userId,
                    $"Market alert: {drop} in {integrationEvent.WindowDays} days",
                    message,
                    NotificationType.Warning), cancellationToken);

                if (result.IsSuccess)
                {
                    notified++;
                }
                else
                {
                    logger.LogError("MarketCrash: failed to notify user {UserId}: {Error}",
                        userId, result.Errors[0].Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MarketCrash: exception notifying user {UserId}", userId);
            }
        }

        logger.LogInformation("MarketCrash {Index} {Drop}: notified {Count}/{Total} users.",
            integrationEvent.IndexTicker, drop, notified, userIds.Count);
    }
}
