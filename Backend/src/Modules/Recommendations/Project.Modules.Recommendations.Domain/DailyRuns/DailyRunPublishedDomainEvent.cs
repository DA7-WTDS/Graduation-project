using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.DailyRuns;

/// <summary>
/// Raised when a run becomes visible to users — either ingested straight to
/// Published, or flipped there later by an operator. Drives the user fanout.
/// </summary>
public sealed record DailyRunPublishedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid DailyRunId,
    DateTime GeneratedAt) : DomainEvent(Id, OccurredOnUtc);
