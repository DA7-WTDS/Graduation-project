using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.DailyRuns;

public sealed record DailyRunIngestedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid DailyRunId,
    DateTime GeneratedAt) : DomainEvent(Id, OccurredOnUtc);
