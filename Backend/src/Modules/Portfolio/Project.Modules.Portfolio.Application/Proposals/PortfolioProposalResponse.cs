using Project.Modules.Portfolio.Domain.Proposals;

namespace Project.Modules.Portfolio.Application.Proposals;

public sealed record ProposalPositionDto(
    string Symbol,
    string Sleeve,
    double Weight,
    decimal EstimatedValue,
    string Rationale);

public sealed record PortfolioProposalResponse(
    Guid Id,
    Guid GoalId,
    int Version,
    string Status,
    string TemplateKey,
    string TemplateName,
    string RebalanceCadence,
    double DrawdownAlertPct,
    string RiskBand,
    int EffectiveRisk,
    decimal Amount,
    IReadOnlyList<ProposalPositionDto> Positions,
    IReadOnlyList<string> Assumptions,
    string InputsHash,
    DateTime CreatedAt,
    DateTime? AcceptedAt)
{
    public static PortfolioProposalResponse From(PortfolioProposal p) => new(
        p.Id,
        p.GoalId,
        p.Version,
        p.Status.ToString(),
        p.TemplateKey,
        p.TemplateName,
        p.RebalanceCadence,
        p.DrawdownAlertPct,
        p.RiskBand.ToString(),
        p.EffectiveRisk,
        p.Amount,
        p.GetPositions()
            .Select(pos => new ProposalPositionDto(pos.Symbol, pos.Sleeve, pos.Weight, pos.EstimatedValue, pos.Rationale))
            .ToList(),
        p.GetAssumptions(),
        p.InputsHash,
        p.CreatedAt,
        p.AcceptedAt);
}
