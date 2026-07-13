using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Allocation;

namespace Project.Modules.Portfolio.Application.Goals.GetPortfolioDraft;

/// <summary>
/// Profile → template → optimizer, on demand (§ 3.2 + § 3.3). Pure read: the
/// draft is recomputed from current registry + latest run each call and never
/// persisted — creating a proposal (Phase 4) is the write path. Deterministic
/// given the same inputs: the InputsHash is the audit anchor and matches the
/// proposal a user would create from identical state.
/// </summary>
internal sealed class GetPortfolioDraftQueryHandler(PortfolioProposalBuilder builder)
    : IQueryHandler<GetPortfolioDraftQuery, PortfolioDraftResponse>
{
    public async Task<Result<PortfolioDraftResponse>> Handle(
        GetPortfolioDraftQuery request, CancellationToken cancellationToken)
    {
        Result<BuiltAllocation> built = await builder.BuildAsync(request.UserId, request.GoalId, cancellationToken);
        if (built.IsFailed)
        {
            return Result.Fail(built.Errors);
        }

        BuiltAllocation b = built.Value;

        return Result.Ok(new PortfolioDraftResponse(
            b.Goal.Id,
            b.Template.Key,
            b.Template.Name,
            b.Template.RebalanceCadence,
            b.Template.DrawdownAlertPct,
            b.Profile.RiskBand.ToString(),
            b.Profile.EffectiveRisk,
            b.Amount,
            b.Allocation.Positions
                .Select(p => new DraftPosition(p.Symbol, p.Sleeve, p.Weight, p.EstimatedValue, p.Rationale))
                .ToList(),
            b.Allocation.Assumptions,
            b.Allocation.InputsHash));
    }
}
