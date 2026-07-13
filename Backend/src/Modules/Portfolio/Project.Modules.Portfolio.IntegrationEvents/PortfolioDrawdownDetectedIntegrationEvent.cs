using Project.Common.Application.EventBus;

namespace Project.Modules.Portfolio.IntegrationEvents;

/// <summary>A user's live portfolio fell past its drawdown threshold from the
/// high-water mark (§ 3.5). Fired once per crossing, re-armed on recovery.</summary>
public sealed record PortfolioDrawdownDetectedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId,
    Guid GoalId,
    double DrawdownPct,
    double ThresholdPct
) : IntegrationEvent(Id, OccurredOnUtc);
