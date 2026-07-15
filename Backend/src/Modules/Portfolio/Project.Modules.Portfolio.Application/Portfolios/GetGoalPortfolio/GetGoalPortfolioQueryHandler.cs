using FluentResults;
using Project.Common.Application.Messaging;
using Project.Common.Domain;
using Project.Common.Domain.Abstractions;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Proposals;
using static Project.Modules.Portfolio.Domain.Goals.GoalErrors;

namespace Project.Modules.Portfolio.Application.Portfolios.GetGoalPortfolio;

/// <summary>
/// The live view of an accepted portfolio (Phase 4.4): marks the book to market
/// against the registry's latest closes so the user sees today's value, not just
/// last night's. If any symbol is unpriced right now we fall back to the last
/// nightly valuation rather than reporting a partial NAV as if it were whole.
/// </summary>
internal sealed class GetGoalPortfolioQueryHandler(
    IGoalRepository goalRepository,
    IGoalPortfolioRepository portfolioRepository,
    IPortfolioProposalRepository proposalRepository,
    IInstrumentRepository instrumentRepository)
    : IQueryHandler<GetGoalPortfolioQuery, GoalPortfolioResponse>
{
    private const string Market = "us"; // second instance per D5 when EGX activates

    public static Error NoActivePortfolio =>
        new Error("This goal has no accepted portfolio yet. Accept a proposal to start tracking it.")
            .WithErrorType(ErrorType.NotFound);

    public async Task<Result<GoalPortfolioResponse>> Handle(
        GetGoalPortfolioQuery request, CancellationToken cancellationToken)
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

        GoalPortfolio? portfolio = await portfolioRepository.GetActiveByGoalIdAsync(request.GoalId, cancellationToken);
        if (portfolio is null)
        {
            return Result.Fail(NoActivePortfolio);
        }

        PortfolioProposal? proposal = await proposalRepository.GetByIdAsync(portfolio.ProposalId, cancellationToken);

        IReadOnlyList<Instrument> instruments = await instrumentRepository.GetActiveByMarketAsync(Market, cancellationToken);
        var priceBySymbol = instruments
            .Where(i => i.LastClose is > 0)
            .ToDictionary(i => i.Symbol, i => i.LastClose!.Value, StringComparer.OrdinalIgnoreCase);

        List<PortfolioHolding> holdings = portfolio.Holdings.ToList();
        bool pricesComplete = holdings.Count > 0 && holdings.All(h => priceBySymbol.ContainsKey(h.Symbol));

        double nav = pricesComplete
            ? PortfolioValuation.Nav(holdings.Select(h => (h.Shares, priceBySymbol[h.Symbol])))
            : portfolio.LastNav;

        // The high-water mark only ever ratchets up (the nightly job owns it), so
        // a live mark above it simply reads as no drawdown.
        double drawdown = PortfolioValuation.Drawdown(nav, portfolio.HighWaterMarkNav);
        double totalReturn = portfolio.Amount > 0 ? nav / (double)portfolio.Amount - 1 : 0;

        var positions = holdings
            .Select(h =>
            {
                double? price = priceBySymbol.TryGetValue(h.Symbol, out double p) ? p : null;
                double? value = price is null ? null : h.Shares * price.Value;
                double? actual = value is null || nav <= 0 || !pricesComplete ? null : value / nav;
                return new LivePositionResponse(
                    h.Symbol,
                    h.Sleeve,
                    h.Shares,
                    h.EntryPrice,
                    price,
                    value,
                    h.TargetWeight,
                    actual,
                    actual is null ? null : actual.Value - h.TargetWeight);
            })
            .OrderByDescending(p => p.TargetWeight)
            .ThenBy(p => p.Symbol, StringComparer.Ordinal)
            .ToList();

        string cadence = proposal?.RebalanceCadence ?? "monthly";

        return Result.Ok(new GoalPortfolioResponse(
            portfolio.GoalId,
            portfolio.ProposalId,
            proposal?.TemplateKey ?? "unknown",
            proposal?.TemplateName ?? "Unknown template",
            cadence,
            portfolio.Amount,
            portfolio.InceptionDate,
            ReviewSchedule.NextReview(portfolio.InceptionDate, cadence, DateTime.UtcNow),
            nav,
            portfolio.HighWaterMarkNav,
            drawdown,
            totalReturn,
            pricesComplete ? DateTime.UtcNow : portfolio.LastValuedAt,
            pricesComplete,
            portfolio.DrawdownThreshold,
            portfolio.DrawdownAlertActive,
            portfolio.DriftAlertActive,
            positions));
    }
}
