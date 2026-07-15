namespace Project.Modules.Portfolio.Application.Portfolios.GetGoalPortfolio;

/// <summary>One live position. CurrentPrice/ActualWeight/DriftPct are null when
/// the registry has no price for the symbol right now (the view never invents
/// one — it says "unknown" instead).</summary>
public sealed record LivePositionResponse(
    string Symbol,
    string Sleeve,
    double Shares,
    double EntryPrice,
    double? CurrentPrice,
    double? CurrentValue,
    double TargetWeight,
    double? ActualWeight,
    double? DriftPct);

public sealed record GoalPortfolioResponse(
    Guid GoalId,
    Guid ProposalId,
    string TemplateKey,
    string TemplateName,
    string RebalanceCadence,
    decimal Amount,
    DateTime InceptionDate,
    DateTime NextReviewDate,

    /// <summary>Live mark when the whole book is priced; otherwise the last
    /// nightly valuation (see <see cref="PricesComplete"/>).</summary>
    double Nav,
    double HighWaterMarkNav,
    double DrawdownPct,
    double TotalReturnPct,
    DateTime? ValuedAt,
    bool PricesComplete,

    double DrawdownThreshold,
    bool DrawdownAlertActive,
    bool DriftAlertActive,

    IReadOnlyList<LivePositionResponse> Positions);
