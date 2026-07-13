using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Portfolio.IntegrationEvents;
using Project.Modules.Portfolio.PublicApi;

namespace Project.Modules.Notifications.Presentation.Portfolios;

/// <summary>
/// Allocation-drift alert (§ 3.5): price moves pulled the portfolio away from
/// its target weights enough to warrant rebalancing. A rebalance nudge — skipped
/// for set-and-forget investors, whose templates rebalance on a slow cadence and
/// who explicitly asked not to be prompted to act.
/// </summary>
public sealed class PortfolioDriftDetectedIntegrationEventHandler(
    ISender sender,
    IPortfolioApi portfolioApi,
    ILogger<PortfolioDriftDetectedIntegrationEventHandler> logger)
    : IntegrationEventHandler<PortfolioDriftDetectedIntegrationEvent>
{
    public override async Task Handle(PortfolioDriftDetectedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        MonitoringProfileResponse? profile =
            await portfolioApi.GetMonitoringProfileAsync(integrationEvent.UserId, cancellationToken);

        if (string.Equals(profile?.Engagement, "SetAndForget", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("PortfolioDrift: user {UserId} is set-and-forget; skipped.", integrationEvent.UserId);
            return;
        }

        string drift = integrationEvent.MaxDriftPct.ToString("P0");
        Result<Guid> result = await sender.Send(new CreateNotificationCommand(
            integrationEvent.UserId,
            "Your portfolio has drifted from its target",
            $"Market moves have pushed one of your positions about {drift} away from its target weight. " +
            "When you're ready, generate a fresh proposal from your goal to rebalance back to plan.",
            NotificationType.Info), cancellationToken);

        if (result.IsFailed)
        {
            logger.LogError("PortfolioDrift: failed to notify user {UserId}: {Error}",
                integrationEvent.UserId, result.Errors[0].Message);
        }
        else
        {
            logger.LogInformation("PortfolioDrift: notified user {UserId} ({Drift}).",
                integrationEvent.UserId, drift);
        }
    }
}
