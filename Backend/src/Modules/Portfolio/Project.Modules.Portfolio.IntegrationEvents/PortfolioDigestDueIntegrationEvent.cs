using Project.Common.Application.EventBus;

namespace Project.Modules.Portfolio.IntegrationEvents;

/// <summary>A goal's periodic portfolio summary is due (§ 3.5). Paced by the
/// user's engagement answer — quarterly for set-and-forget, monthly otherwise.</summary>
public sealed record PortfolioDigestDueIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid UserId,
    Guid GoalId,
    string TemplateName,
    string Engagement,
    int PeriodDays,
    double Nav,
    double TotalReturnPct,
    double DrawdownPct,
    DateTime NextReviewDate
) : IntegrationEvent(Id, OccurredOnUtc);
