using Project.Common.Application.EventBus;

namespace Project.Modules.Recommendations.IntegrationEvents;

/// <summary>A user's held position(s) newly flipped — model says DOWN and
/// sentiment turned NEGATIVE (§ 3.5). One event per affected user per run.</summary>
public sealed record ConvictionReversalDetectedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId,
    List<string> Tickers,
    DateTime RunGeneratedAt
) : IntegrationEvent(Id, OccurredOnUtc);
