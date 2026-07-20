using System.Globalization;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Recommendations.IntegrationEvents;
using Project.Modules.Users.PublicApi;

namespace Project.Modules.Notifications.Presentation.Recommendations;

/// <summary>
/// § 6.2 ops alert: when a run lands anywhere other than Published (quarantined
/// by the quality gates, or pending manual approval), every Admin user gets a
/// notification so a human reviews it. User-facing fanout lives in
/// <see cref="DailyRunPublishedIntegrationEventHandler"/>.
/// </summary>
public sealed class DailyRunIngestedIntegrationEventHandler(
    ISender sender,
    IUsersApi usersApi,
    ILogger<DailyRunIngestedIntegrationEventHandler> logger)
    : IntegrationEventHandler<DailyRunIngestedIntegrationEvent>
{
    public override async Task Handle(DailyRunIngestedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (string.Equals(integrationEvent.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            // Clean auto-published run — the Published event drives the user fanout.
            return;
        }

        bool quarantined = string.Equals(integrationEvent.Status, "Quarantined", StringComparison.OrdinalIgnoreCase);
        string runDate = integrationEvent.GeneratedAt.ToString("MMMM d", CultureInfo.InvariantCulture);

        string title = quarantined
            ? "Daily run QUARANTINED — data-quality gates failed"
            : "Daily run pending review";

        string message = quarantined
            ? $"The {runDate} pipeline run failed quality gates and was quarantined: " +
              $"{integrationEvent.StatusReason ?? "no reason recorded"}. It is invisible to users. " +
              $"Review it and publish or leave it quarantined (run {integrationEvent.DailyRunId})."
            : $"The {runDate} pipeline run passed quality gates and is awaiting manual approval " +
              $"(run {integrationEvent.DailyRunId}). Users keep seeing the previous published run until you publish it.";

        IReadOnlyList<Guid> adminIds = await usersApi.GetAdminUserIdsAsync(cancellationToken);

        if (adminIds.Count == 0)
        {
            logger.LogWarning(
                "DailyRunIngested {DailyRunId} landed as {Status} but there are no Admin users to alert. Reason: {Reason}",
                integrationEvent.DailyRunId, integrationEvent.Status, integrationEvent.StatusReason);
            return;
        }

        int notified = 0;
        foreach (Guid adminId in adminIds)
        {
            Result<Guid> result = await sender.Send(new CreateNotificationCommand(
                adminId,
                title,
                message,
                NotificationType.Warning), cancellationToken);

            if (result.IsFailed)
            {
                logger.LogError("Failed to create ops alert for admin {AdminId}: {Error}",
                    adminId, result.Errors[0].Message);
            }
            else
            {
                notified++;
            }
        }

        logger.LogInformation(
            "DailyRunIngested {DailyRunId} ({Status}): ops alert sent to {Count}/{Total} admins.",
            integrationEvent.DailyRunId, integrationEvent.Status, notified, adminIds.Count);
    }
}
