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
/// Conviction-reversal alert (§ 3.5): a held position newly flipped — the model
/// turned bearish AND the news turned negative. Audience is active profiles
/// only; set-and-forget investors opted out of position-level noise, and their
/// templates hold no single stocks anyway.
/// </summary>
public sealed class ConvictionReversalDetectedIntegrationEventHandler(
    ISender sender,
    IPortfolioApi portfolioApi,
    ILogger<ConvictionReversalDetectedIntegrationEventHandler> logger)
    : IntegrationEventHandler<ConvictionReversalDetectedIntegrationEvent>
{
    public override async Task Handle(ConvictionReversalDetectedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        MonitoringProfileResponse? profile =
            await portfolioApi.GetMonitoringProfileAsync(integrationEvent.UserId, cancellationToken);

        if (string.Equals(profile?.Engagement, "SetAndForget", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("ConvictionReversal: user {UserId} is set-and-forget; skipped.", integrationEvent.UserId);
            return;
        }

        string tickers = string.Join(", ", integrationEvent.Tickers);
        string plural = integrationEvent.Tickers.Count == 1 ? "position" : "positions";

        Result<Guid> result = await sender.Send(new CreateNotificationCommand(
            integrationEvent.UserId,
            $"Signal reversal on {tickers}",
            $"Today's run flipped on your held {plural} {tickers}: the model now points down and news sentiment " +
            "turned negative at the same time. That combination is worth a look — open your dashboard to review " +
            "the rationale and decide whether the original reason you bought still holds.",
            NotificationType.Warning), cancellationToken);

        if (result.IsFailed)
        {
            logger.LogError("ConvictionReversal: failed to notify user {UserId}: {Error}",
                integrationEvent.UserId, result.Errors[0].Message);
        }
        else
        {
            logger.LogInformation("ConvictionReversal: notified user {UserId} about {Tickers}.",
                integrationEvent.UserId, tickers);
        }
    }
}
