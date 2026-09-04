using System.Globalization;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Portfolio.IntegrationEvents;
using Project.Modules.Users.PublicApi;

namespace Project.Modules.Notifications.Presentation.Portfolios;

/// <summary>
/// Ops alert: the nightly shadow-portfolio run produced no snapshots, so the
/// public model-portfolio track record didn't advance. Notifies every Admin —
/// a missed night should be visible, not silent.
/// </summary>
public sealed class ShadowRunBlockedIntegrationEventHandler(
    ISender sender,
    IUsersApi usersApi,
    ILogger<ShadowRunBlockedIntegrationEventHandler> logger)
    : IntegrationEventHandler<ShadowRunBlockedIntegrationEvent>
{
    public override async Task Handle(ShadowRunBlockedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> adminIds = await usersApi.GetAdminUserIdsAsync(cancellationToken);
        if (adminIds.Count == 0)
        {
            logger.LogWarning(
                "ShadowRunBlocked ({Market}): no snapshots written for {Count} portfolio(s) — {Reason} — and no Admin users to alert.",
                integrationEvent.Market, integrationEvent.PortfolioCount, integrationEvent.Reason);
            return;
        }

        string day = integrationEvent.OccurredOnUtc.ToString("MMMM d", CultureInfo.InvariantCulture);
        string message =
            $"The {integrationEvent.Market.ToUpperInvariant()} model-portfolio track record did not advance on {day}: " +
            $"{integrationEvent.Reason}. {integrationEvent.PortfolioCount} portfolio(s) affected. " +
            "Check the pipeline run and instrument prices, then re-run via /api/internal/shadow/run.";

        foreach (Guid adminId in adminIds)
        {
            Result<Guid> result = await sender.Send(new CreateNotificationCommand(
                adminId,
                "Shadow track record did not update",
                message,
                NotificationType.Warning), cancellationToken);

            if (result.IsFailed)
            {
                logger.LogError("Failed to create shadow-run ops alert for admin {AdminId}: {Error}",
                    adminId, result.Errors[0].Message);
            }
        }

        logger.LogInformation("ShadowRunBlocked ({Market}): ops alert sent to {Count} admin(s).",
            integrationEvent.Market, adminIds.Count);
    }
}
