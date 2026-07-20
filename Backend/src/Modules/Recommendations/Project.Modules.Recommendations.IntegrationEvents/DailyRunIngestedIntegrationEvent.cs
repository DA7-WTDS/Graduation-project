using Project.Common.Application.EventBus;

namespace Project.Modules.Recommendations.IntegrationEvents;

public sealed record DailyRunIngestedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid DailyRunId,
    DateTime GeneratedAt,
    string Status,
    string? StatusReason
) : IntegrationEvent(Id, OccurredOnUtc);
