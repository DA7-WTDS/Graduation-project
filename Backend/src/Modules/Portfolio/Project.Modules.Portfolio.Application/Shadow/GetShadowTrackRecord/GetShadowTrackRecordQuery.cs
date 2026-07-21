using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Shadow.GetShadowTrackRecord;

/// <summary>
/// Public shadow track record (§ 6.1): each strategy template's model-portfolio
/// NAV history and headline stats. Every series is "costs simulated" — FRA-safe
/// wording the UI must preserve.
/// </summary>
public sealed record GetShadowTrackRecordQuery : IQuery<ShadowTrackRecordResponse>;

public sealed record ShadowTrackRecordResponse(
    string Disclaimer,
    IReadOnlyList<ShadowSeries> Portfolios);

public sealed record ShadowSeries(
    string TemplateKey,
    string TemplateName,
    string RiskBand,
    string RebalanceCadence,
    decimal Notional,
    DateOnly InceptionDate,
    double CurrentNav,
    double TotalReturn,
    double AnnualizedReturn,
    double MaxDrawdown,
    int Days,
    IReadOnlyList<ShadowNavPoint> Series);

public sealed record ShadowNavPoint(DateOnly Date, double Nav, double DailyReturn, bool Rebalanced);
