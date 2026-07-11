namespace Project.Modules.Portfolio.Application.Goals.GetPortfolioDraft;

public sealed record DraftPosition(
    string Symbol,
    string Sleeve,
    double Weight,
    decimal EstimatedValue,
    string Rationale);

public sealed record PortfolioDraftResponse(
    Guid GoalId,
    string TemplateKey,
    string TemplateName,
    string RebalanceCadence,
    double DrawdownAlertPct,
    string RiskBand,
    int EffectiveRisk,
    decimal Amount,
    IReadOnlyList<DraftPosition> Positions,
    IReadOnlyList<string> Assumptions,
    string InputsHash);
