using Project.Common.Application.EventBus;

namespace Project.Modules.Recommendations.IntegrationEvents;

/// <summary>The market index dropped past the crash threshold (§ 3.5). Fired
/// once per crossing, not per night the condition persists.</summary>
public sealed record MarketCrashDetectedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string IndexTicker,
    double DropPct,
    int WindowDays,
    DateTime AsOfDate
) : IntegrationEvent(Id, OccurredOnUtc);
