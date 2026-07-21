using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;
using Project.Modules.Portfolio.Application.Abstractions.Strategies;
using Project.Modules.Portfolio.Domain.Allocation;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Shadow;
using Project.Modules.Portfolio.Domain.Strategies;
using Project.Modules.Recommendations.PublicApi;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

/// <summary>
/// Nightly shadow-portfolio run (§ 6.1): every strategy template is a live paper
/// portfolio at a fixed notional, so the public track record launches with real
/// history from before we have users.
///
/// After the 03:30 valuation:
///   1. create a shadow portfolio for any active template that lacks one;
///   2. for each, if the whole book is priced tonight, mark it to market;
///   3. on a rebalance-cadence day (or at inception), run the same optimizer a
///      user gets (§ 3.3) and trade to it with the § 1.8 cost model;
///   4. write one snapshot, and log a drawdown alert on the crossing edge.
///
/// A portfolio missing any price tonight is skipped and revalued tomorrow —
/// never valued on a partial book (same rule as the live valuation job).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class ShadowPortfolioJob(
    IShadowPortfolioRepository shadowRepository,
    IStrategyTemplateRepository templateRepository,
    IInstrumentRepository instrumentRepository,
    IRecommendationsApi recommendationsApi,
    IUnitOfWork unitOfWork,
    IOptions<ShadowPortfolioOptions> options,
    ILogger<ShadowPortfolioJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ShadowPortfolioOptions opts = options.Value;
        CancellationToken ct = context.CancellationToken;
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        IReadOnlyList<StrategyTemplate> templates = await templateRepository.GetActiveAsync(ct);
        if (templates.Count == 0)
        {
            logger.LogInformation("ShadowPortfolioJob — no active templates.");
            return;
        }

        // Ensure one shadow portfolio per active template (idempotent).
        IReadOnlyList<ShadowPortfolio> existing = await shadowRepository.GetAllForMarketAsync(opts.Market, ct);
        var byKey = existing.ToDictionary(p => p.TemplateKey, StringComparer.OrdinalIgnoreCase);

        foreach (StrategyTemplate template in templates)
        {
            if (byKey.ContainsKey(template.Key))
            {
                continue;
            }

            var created = ShadowPortfolio.Create(
                template.Key, template.Name, opts.Market,
                ShadowRiskBand.ForTemplate(template.RiskMin, template.RiskMax),
                template.RebalanceCadence, template.DrawdownAlertPct,
                opts.Notional, today);

            await shadowRepository.AddAsync(created, ct);
            existing = [.. existing, created];
            logger.LogInformation("ShadowPortfolioJob — created shadow portfolio for template {Template}.", template.Key);
        }

        IReadOnlyList<Instrument> instruments = await instrumentRepository.GetActiveByMarketAsync(opts.Market, ct);
        var priceBySymbol = instruments
            .Where(i => i.LastClose is > 0)
            .ToDictionary(i => i.Symbol, i => i.LastClose!.Value, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<RankedTicker> ranked = await recommendationsApi.GetLatestRankedTickersAsync(ct);
        List<RankedEquity> rankings = ranked
            .Select(r => new RankedEquity(r.Ticker, r.ConvictionScore, r.Direction, r.RiskLevel, r.Signal, r.Rsi14, r.PctVsSma50))
            .ToList();

        var templateByKey = templates.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);
        int valued = 0, rebalanced = 0, skipped = 0, drawdownAlerts = 0;

        foreach (ShadowPortfolio portfolio in existing)
        {
            if (await shadowRepository.SnapshotExistsAsync(portfolio.Id, today, ct))
            {
                continue; // already ran today — idempotent across re-runs
            }

            bool priced = portfolio.Positions.All(p => priceBySymbol.ContainsKey(p.Symbol));
            bool dueRebalance = !portfolio.IsInvested
                || ReviewSchedule.NextReview(RebalanceAnchor(portfolio), portfolio.RebalanceCadence, DateTime.UtcNow.AddDays(-1)) <= DateTime.UtcNow;

            if (!portfolio.IsInvested)
            {
                // Inception buy needs every target symbol priced tonight.
                if (!templateByKey.TryGetValue(portfolio.TemplateKey, out StrategyTemplate? tpl)
                    || !await TryRebalanceAsync(portfolio, tpl, instruments, rankings, priceBySymbol, opts, today, ct))
                {
                    skipped++;
                    continue;
                }
                rebalanced++;
                valued++;
                continue;
            }

            if (!priced)
            {
                skipped++;
                continue; // incomplete book — revalue tomorrow
            }

            if (dueRebalance && templateByKey.TryGetValue(portfolio.TemplateKey, out StrategyTemplate? template2)
                && await TryRebalanceAsync(portfolio, template2, instruments, rankings, priceBySymbol, opts, today, ct))
            {
                rebalanced++;
            }
            else
            {
                double nav = ShadowRebalancer.Nav(
                    portfolio.Positions.Select(p => new ShadowLot(p.Symbol, p.Sleeve, p.Shares, p.AvgCost)),
                    priceBySymbol, portfolio.CashBalance);
                double dailyReturn = portfolio.LastNav > 0 ? nav / portfolio.LastNav - 1 : 0;
                portfolio.ApplyValuation(nav, today);
                await shadowRepository.AddSnapshotAsync(
                    ShadowSnapshot.Create(portfolio.Id, today, nav, dailyReturn, rebalanced: false), ct);
            }

            if (LogDrawdown(portfolio))
            {
                drawdownAlerts++;
            }

            valued++;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ShadowPortfolioJob — valued {Valued}, rebalanced {Rebalanced}, skipped {Skipped} (missing prices), drawdown alerts {Alerts}.",
            valued, rebalanced, skipped, drawdownAlerts);
    }

    /// <summary>The cadence clock runs from the last rebalance, or from inception
    /// before the first one.</summary>
    private static DateTime RebalanceAnchor(ShadowPortfolio p) =>
        (p.LastRebalancedOn ?? p.InceptionDate).ToDateTime(TimeOnly.MinValue);

    private async Task<bool> TryRebalanceAsync(
        ShadowPortfolio portfolio,
        StrategyTemplate template,
        IReadOnlyList<Instrument> instruments,
        IReadOnlyList<RankedEquity> rankings,
        IReadOnlyDictionary<string, double> priceBySymbol,
        ShadowPortfolioOptions opts,
        DateOnly today,
        CancellationToken ct)
    {
        double navBefore = ShadowRebalancer.Nav(
            portfolio.Positions.Select(p => new ShadowLot(p.Symbol, p.Sleeve, p.Shares, p.AvgCost)),
            priceBySymbol, portfolio.CashBalance);

        AllocationResult allocation = AllocationOptimizer.Build(
            template.GetBuckets(), portfolio.RiskBand, instruments, rankings, (decimal)navBefore);

        var targets = allocation.Positions
            .Select(p => new ShadowTarget(p.Symbol, p.Sleeve, p.Weight))
            .ToList();

        // Every target symbol must be priced tonight, or we cannot buy it.
        if (targets.Count == 0 || targets.Any(t => !priceBySymbol.ContainsKey(t.Symbol)))
        {
            return false;
        }

        RebalanceResult result = ShadowRebalancer.Rebalance(
            portfolio.Positions.Select(p => new ShadowLot(p.Symbol, p.Sleeve, p.Shares, p.AvgCost)),
            targets, priceBySymbol, portfolio.CashBalance, opts.CostPerSide);

        double dailyReturn = portfolio.LastNav > 0 ? result.NavAfter / portfolio.LastNav - 1 : 0;
        portfolio.ApplyRebalance(result.Lots, result.Cash, result.NavAfter, today);
        await shadowRepository.AddSnapshotAsync(
            ShadowSnapshot.Create(portfolio.Id, today, result.NavAfter, dailyReturn, rebalanced: true), ct);

        return true;
    }

    private bool LogDrawdown(ShadowPortfolio portfolio)
    {
        double drawdown = PortfolioValuation.Drawdown(portfolio.LastNav, portfolio.HighWaterMarkNav);
        if (portfolio.EvaluateDrawdownAlert(drawdown))
        {
            logger.LogWarning(
                "ShadowPortfolioJob — {Template} crossed its drawdown alert: {Drawdown:P1} below high-water mark (threshold {Threshold:P0}).",
                portfolio.TemplateKey, drawdown, portfolio.DrawdownAlertPct);
            return true;
        }

        return false;
    }
}
