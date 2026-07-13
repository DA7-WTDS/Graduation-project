using Project.Common.Application.EventBus;

namespace Project.Modules.Portfolio.IntegrationEvents;

/// <summary>A user's live portfolio drifted past the rebalance threshold from
/// its target weights (§ 3.5). A rebalance nudge, fired once per crossing.</summary>
public sealed record PortfolioDriftDetectedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId,
    Guid GoalId,
    double MaxDriftPct,
    double ThresholdPct
) : IntegrationEvent(Id, OccurredOnUtc);
