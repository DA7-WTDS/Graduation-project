using FluentResults;
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

namespace Project.Modules.Portfolio.Application.Allocation;

/// <summary>The optimizer run for one goal, resolved from current state
/// (registry + latest ranking run + the goal's profile).</summary>
public sealed record BuiltAllocation(
    Goal Goal,
    StrategyTemplate Template,
    InvestorProfile Profile,
    decimal Amount,
    AllocationResult Allocation);

/// <summary>
/// Shared "goal → portfolio" pipeline (§ 3.2 + § 3.3): resolves the goal's
/// profile, selects the template, and runs the deterministic optimizer against
/// the live registry and latest ranking run. The draft preview and the persisted
/// proposal (Phase 4) both go through here, so a preview and the proposal a user
/// then creates from identical state carry the same InputsHash — never divergent.
/// </summary>
public sealed class PortfolioProposalBuilder(
    IGoalRepository goalRepository,
    IStrategyTemplateRepository templateRepository,
    IInstrumentRepository instrumentRepository,
    IPortfolioRepository portfolioRepository,
    IRecommendationsApi recommendationsApi)
{
    private const string Market = "us"; // second instance per D5 when EGX activates

    public async Task<Result<BuiltAllocation>> BuildAsync(
        Guid userId, Guid goalId, CancellationToken cancellationToken)
    {
        Goal? goal = await goalRepository.GetByIdAsync(goalId, cancellationToken);
        if (goal is null)
        {
            return Result.Fail(GoalNotFound(goalId));
        }

        if (goal.UserId != userId)
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
            .Select(r => new RankedEquity(
                r.Ticker, r.ConvictionScore, r.Direction, r.RiskLevel,
                r.Signal, r.Rsi14, r.PctVsSma50))
            .ToList();

        decimal amount = (await portfolioRepository.GetByUserIdAsync(userId, cancellationToken))
            ?.InvestmentAmount ?? 0m;

        AllocationResult allocation = AllocationOptimizer.Build(
            template.GetBuckets(), profile.RiskBand, instruments, rankings, amount);

        return Result.Ok(new BuiltAllocation(goal, template, profile, amount, allocation));
    }
}
