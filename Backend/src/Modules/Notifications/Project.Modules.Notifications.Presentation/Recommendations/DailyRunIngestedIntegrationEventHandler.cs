using System.Globalization;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Presentation.Ops;
using Project.Modules.Recommendations.IntegrationEvents;

namespace Project.Modules.Notifications.Presentation.Recommendations;

/// <summary>
/// § 6.2 ops alert: a run landed anywhere other than Published — quarantined by the
/// quality gates, or held for manual approval — so a human needs to look at it.
/// User-facing fanout lives in <see cref="DailyRunPublishedIntegrationEventHandler"/>.
/// </summary>
public sealed class DailyRunIngestedIntegrationEventHandler(IOpsAlert opsAlert)
    : IntegrationEventHandler<DailyRunIngestedIntegrationEvent>
{
    public override Task Handle(
        DailyRunIngestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(integrationEvent.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            // Clean auto-published run — the Published event drives the user fanout.
            return Task.CompletedTask;
        }

        bool quarantined = string.Equals(integrationEvent.Status, "Quarantined", StringComparison.OrdinalIgnoreCase);
        string runDate = integrationEvent.GeneratedAt.ToString("MMMM d", CultureInfo.InvariantCulture);
        string reason = integrationEvent.StatusReason ?? "no reason recorded";

        string title = quarantined
            ? "Daily run QUARANTINED — data-quality gates failed"
            : "Daily run pending review";

        string message = quarantined
            ? $"The {runDate} pipeline run failed quality gates and was quarantined: {reason}. " +
              $"It is invisible to users. Review it and publish, or leave it quarantined " +
              $"(run {integrationEvent.DailyRunId})."
            : $"The {runDate} pipeline run passed quality gates and is awaiting manual approval " +
              $"(run {integrationEvent.DailyRunId}). Users keep seeing the previous published run " +
              $"until you publish it.";

        return opsAlert.RaiseAsync(title, message, cancellationToken);
    }
}
