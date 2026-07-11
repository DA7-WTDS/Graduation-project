using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Strategies;
using Project.Modules.Portfolio.Domain.Allocation;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Strategies;
using Project.Modules.Recommendations.PublicApi;
using static Project.Modules.Portfolio.Domain.Goals.GoalErrors;

namespace Project.Modules.Portfolio.Application.Goals.GetPortfolioDraft;

/// <summary>
/// Profile → template → optimizer, on demand (§ 3.2 + § 3.3). Pure read: the
/// draft is recomputed from current registry + latest run each call; persisting
/// an accepted portfolio is Phase 4 work. Deterministic given the same inputs —
/// the InputsHash in the response is the audit anchor.
/// </summary>
internal sealed class GetPortfolioDraftQueryHandler(
    IGoalRepository goalRepository,
    IStrategyTemplateRepository templateRepository,
    IInstrumentRepository instrumentRepository,
    IPortfolioRepository portfolioRepository,
    IRecommendationsApi recommendationsApi)
    : IQueryHandler<GetPortfolioDraftQuery, PortfolioDraftResponse>
{
    private const string Market = "us"; // second instance per D5 when EGX activates

    public async Task<Result<PortfolioDraftResponse>> Handle(
        GetPortfolioDraftQuery request, CancellationToken cancellationToken)
    {
        Goal? goal = await goalRepository.GetByIdAsync(request.GoalId, cancellationToken);
        if (goal is null)
        {
            return Result.Fail(GoalNotFound(request.GoalId));
        }

        if (goal.UserId != request.UserId)
        {
            return Result.Fail(UnauthorizedAccess);
        }

        InvestorProfile? profile = await goalRepository.GetLatestProfileAsync(goal.Id, cancellationToken);
        if (profile is null)
        {
            return Result.Fail(ProfileMissing);
        }

        IReadOnlyList<StrategyTemplate> templates = await templateRepository.GetActiveAsync(cancellationToken);
        StrategyTemplate? template = TemplateSelector.Select(
            templates, goal.Type.ToString(), profile.EffectiveRisk, profile.SpeculativeUnlocked);
        if (template is null)
        {
            return Result.Fail(NoTemplateMatches);
        }

        IReadOnlyList<Instrument> instruments = await instrumentRepository.GetActiveByMarketAsync(Market, cancellationToken);

        IReadOnlyList<RankedTicker> ranked = await recommendationsApi.GetLatestRankedTickersAsync(cancellationToken);
        List<RankedEquity> rankings = ranked
            .Select(r => new RankedEquity(r.Ticker, r.ConvictionScore, r.Direction, r.RiskLevel))
            .ToList();

        decimal amount = (await portfolioRepository.GetByUserIdAsync(request.UserId, cancellationToken))
            ?.InvestmentAmount ?? 0m;

        AllocationResult allocation = AllocationOptimizer.Build(
            template.GetBuckets(), profile.RiskBand, instruments, rankings, amount);

        return Result.Ok(new PortfolioDraftResponse(
            goal.Id,
            template.Key,
            template.Name,
            template.RebalanceCadence,
            template.DrawdownAlertPct,
            profile.RiskBand.ToString(),
            profile.EffectiveRisk,
            amount,
            allocation.Positions
                .Select(p => new DraftPosition(p.Symbol, p.Sleeve, p.Weight, p.EstimatedValue, p.Rationale))
                .ToList(),
            allocation.Assumptions,
            allocation.InputsHash));
    }
}
