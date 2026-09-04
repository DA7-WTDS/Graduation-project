using System.Globalization;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Presentation.Ops;
using Project.Modules.Recommendations.IntegrationEvents;

namespace Project.Modules.Notifications.Presentation.Recommendations;

/// <summary>
/// Ops alert (§ 1.7): live accuracy crossed below its floor between monthly retrains.
/// Users keep being served the incumbent model until someone decides otherwise, so this
/// has to reach a person rather than a log file.
/// </summary>
public sealed class ModelDriftDetectedIntegrationEventHandler(IOpsAlert opsAlert)
    : IntegrationEventHandler<ModelDriftDetectedIntegrationEvent>
{
    public override Task Handle(
        ModelDriftDetectedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        string message = string.Format(
            CultureInfo.InvariantCulture,
            "The rolling {0}-day directional hit-rate has fallen to {1:P1} across {2} scored " +
            "predictions, below the {3:P0} floor. Users are still being served the current model. " +
            "Investigate before the next scheduled retrain: check recent daily runs for data-quality " +
            "problems, and compare champion against challenger on the newest out-of-sample slice.",
            integrationEvent.WindowDays,
            integrationEvent.HitRate,
            integrationEvent.SampleSize,
            integrationEvent.Threshold);

        return opsAlert.RaiseAsync("Model drift — live accuracy below floor", message, cancellationToken);
    }
}
