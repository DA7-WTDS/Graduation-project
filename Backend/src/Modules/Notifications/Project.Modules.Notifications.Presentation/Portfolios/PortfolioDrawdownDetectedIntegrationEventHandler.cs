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
/// Portfolio drawdown alert (§ 3.5): the user's own portfolio — not just the
/// market — is down past its threshold. Everyone hears it, tone per profile:
/// set-and-forget / retirement / conservative get "this is expected, hold the
/// plan" reassurance; active profiles get context and a nudge to review.
/// </summary>
public sealed class PortfolioDrawdownDetectedIntegrationEventHandler(
    ISender sender,
    IPortfolioApi portfolioApi,
    ILogger<PortfolioDrawdownDetectedIntegrationEventHandler> logger)
    : IntegrationEventHandler<PortfolioDrawdownDetectedIntegrationEvent>
{
    public override async Task Handle(PortfolioDrawdownDetectedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        MonitoringProfileResponse? profile =
            await portfolioApi.GetMonitoringProfileAsync(integrationEvent.UserId, cancellationToken);

        bool holdGuidance =
            string.Equals(profile?.GoalType, "Retirement", StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile?.Engagement, "SetAndForget", StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile?.RiskProfile, "Conservative", StringComparison.OrdinalIgnoreCase);

        string drop = integrationEvent.DrawdownPct.ToString("P1");
        string message = holdGuidance
            ? $"Your portfolio is down {drop} from its high. A dip of this size is within what your plan expects — " +
              "it does not mean anything is broken. Selling now would lock in the loss; holding is the plan working as designed."
            : $"Your portfolio is down {drop} from its high. Worth a look: open your dashboard to review your positions " +
              "and, if your view has changed, generate a fresh proposal to rebalance.";

        Result<Guid> result = await sender.Send(new CreateNotificationCommand(
            integrationEvent.UserId,
            $"Portfolio down {drop} from its high",
            message,
            NotificationType.Warning), cancellationToken);

        if (result.IsFailed)
        {
            logger.LogError("PortfolioDrawdown: failed to notify user {UserId}: {Error}",
                integrationEvent.UserId, result.Errors[0].Message);
        }
        else
        {
            logger.LogInformation("PortfolioDrawdown: notified user {UserId} ({Drop}).",
                integrationEvent.UserId, drop);
        }
    }
}
