using Project.Common.Application.EventBus;

namespace Project.Modules.Recommendations.IntegrationEvents;

/// <summary>
/// The rolling directional hit-rate crossed below its floor (IMPLEMENTATION_PLAN § 1.7).
///
/// The model is retrained monthly, so this exists to catch degradation BETWEEN retrains,
/// while users are still being served the incumbent. Fired on the crossing night only:
/// a condition that persists for a month should not produce thirty identical alerts,
/// because an alert nobody reads is the same as no alert.
/// </summary>
public sealed record ModelDriftDetectedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    double HitRate,
    double Threshold,
    int SampleSize,
    int WindowDays
) : IntegrationEvent(Id, OccurredOnUtc);
