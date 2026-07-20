using Project.Common.Application.EventBus;

namespace Project.Modules.Recommendations.IntegrationEvents;

/// <summary>
/// A run became visible to users (§ 6.2) — either ingested straight to Published
/// or approved by an operator later. Drives the user "picks are ready" fanout.
/// </summary>
public sealed record DailyRunPublishedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid DailyRunId,
    DateTime GeneratedAt
) : IntegrationEvent(Id, OccurredOnUtc);
