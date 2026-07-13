using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Common.Application.EventBus;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.IntegrationEvents;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

/// <summary>
/// Nightly portfolio valuation + monitoring triggers (§ 3.5, Phase 4.2). Marks
/// every active portfolio to market against the registry's closes, tracks the
/// high-water mark, and fires drawdown / drift events on crossing (the aggregate
/// holds the hysteresis so a persisting condition alerts once, not every night).
/// Portfolios with a missing price tonight are skipped and revalued tomorrow —
/// never valued on a partial book.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class PortfolioValuationJob(
    IGoalPortfolioRepository portfolioRepository,
    IInstrumentRepository instrumentRepository,
    IEventBus eventBus,
    IUnitOfWork unitOfWork,
    IOptions<PortfolioValuationOptions> options,
    ILogger<PortfolioValuationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        PortfolioValuationOptions opts = options.Value;
        CancellationToken ct = context.CancellationToken;

        IReadOnlyList<GoalPortfolio> portfolios = await portfolioRepository.GetAllActiveAsync(ct);
        if (portfolios.Count == 0)
        {
            logger.LogInformation("PortfolioValuationJob — no active portfolios.");
            return;
        }

        IReadOnlyList<Instrument> instruments = await instrumentRepository.GetActiveByMarketAsync(opts.Market, ct);
        var priceBySymbol = instruments
            .Where(i => i.LastClose is > 0)
            .ToDictionary(i => i.Symbol, i => i.LastClose!.Value, StringComparer.OrdinalIgnoreCase);

        DateTime asOf = DateTime.UtcNow;
        int valued = 0, skipped = 0, drawdownEvents = 0, driftEvents = 0;

        foreach (GoalPortfolio portfolio in portfolios)
        {
            List<PortfolioHolding> holdings = portfolio.Holdings.ToList();
            if (holdings.Count == 0 || holdings.Any(h => !priceBySymbol.ContainsKey(h.Symbol)))
            {
                skipped++;
                continue; // incomplete price book — revalue tomorrow
            }

            double nav = PortfolioValuation.Nav(holdings.Select(h => (h.Shares, priceBySymbol[h.Symbol])));
            portfolio.ApplyValuation(nav, asOf);

            double drawdown = PortfolioValuation.Drawdown(nav, portfolio.HighWaterMarkNav);
            double maxDrift = PortfolioValuation.MaxDrift(
                holdings.Select(h => (h.Symbol, h.Shares * priceBySymbol[h.Symbol], h.TargetWeight)));

            if (portfolio.EvaluateDrawdownAlert(drawdown))
            {
                await eventBus.PublishAsync(new PortfolioDrawdownDetectedIntegrationEvent(
                    Guid.NewGuid(), asOf, portfolio.UserId, portfolio.GoalId, drawdown, portfolio.DrawdownThreshold), ct);
                drawdownEvents++;
            }

            if (portfolio.EvaluateDriftAlert(maxDrift, opts.DriftThreshold))
            {
                await eventBus.PublishAsync(new PortfolioDriftDetectedIntegrationEvent(
                    Guid.NewGuid(), asOf, portfolio.UserId, portfolio.GoalId, maxDrift, opts.DriftThreshold), ct);
                driftEvents++;
            }

            valued++;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "PortfolioValuationJob — valued {Valued}, skipped {Skipped} (missing prices), drawdown alerts {Drawdown}, drift alerts {Drift}.",
            valued, skipped, drawdownEvents, driftEvents);
    }
}
