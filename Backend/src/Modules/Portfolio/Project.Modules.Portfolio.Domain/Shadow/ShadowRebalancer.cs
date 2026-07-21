namespace Project.Modules.Portfolio.Domain.Shadow;

/// <summary>A target sleeve weight for one symbol, straight from the optimizer.</summary>
public sealed record ShadowTarget(string Symbol, string Sleeve, double Weight);

/// <summary>One line of the rebalanced book: shares held and the cost basis at
/// which the current lot was established.</summary>
public sealed record ShadowLot(string Symbol, string Sleeve, double Shares, double AvgCost);

public sealed record RebalanceResult(
    IReadOnlyList<ShadowLot> Lots,
    double Cash,
    double NavBefore,
    double NavAfter,
    double TradedValue,
    double Cost);

/// <summary>
/// Pure paper-trading engine for the shadow track record (§ 6.1). Marks the book
/// to today's closes and, on a rebalance day, trades to the optimizer's target
/// weights — deducting the <b>same transaction-cost model the backtester uses
/// (§ 1.8): 25 bps per side on the notional traded</b>. Kept a pure function so
/// the cost arithmetic is unit-checkable against the Python backtester.
/// </summary>
public static class ShadowRebalancer
{
    /// <summary>Backtester parity (§ 1.8: <c>COST_ONE_SIDE = 0.0025</c>).</summary>
    public const double DefaultCostPerSide = 0.0025;

    /// <summary>Mark-to-market only — no trades, no cost. NAV drifts with prices.
    /// Positions whose price is unknown tonight must be excluded by the caller,
    /// never valued at zero (the job skips an incomplete book instead).</summary>
    public static double Nav(IEnumerable<ShadowLot> lots, IReadOnlyDictionary<string, double> prices, double cash) =>
        cash + lots.Sum(l => l.Shares * prices[l.Symbol]);

    /// <summary>
    /// Trade the current book to <paramref name="targets"/> at today's prices.
    ///
    /// Turnover cost mirrors § 1.8 exactly: each dollar of absolute change in a
    /// position's market value is one dollar traded, charged
    /// <paramref name="costPerSide"/> once. Buying the whole book from cash at
    /// inception therefore costs <c>notional × costPerSide</c>. The cost comes out
    /// of NAV, then the book is set to the target weights of the post-cost NAV so
    /// the result is internally consistent (Σ position value + cash == NavAfter).
    /// </summary>
    public static RebalanceResult Rebalance(
        IEnumerable<ShadowLot> current,
        IReadOnlyList<ShadowTarget> targets,
        IReadOnlyDictionary<string, double> prices,
        double cash,
        double costPerSide = DefaultCostPerSide)
    {
        List<ShadowLot> currentList = current.ToList();

        double navBefore = cash + currentList.Sum(l => l.Shares * prices[l.Symbol]);

        // Current vs target market value per symbol; the union covers names being
        // fully exited (target 0) as well as new buys.
        var currentValue = currentList.ToDictionary(
            l => l.Symbol, l => l.Shares * prices[l.Symbol], StringComparer.OrdinalIgnoreCase);
        var targetValue = targets.ToDictionary(
            t => t.Symbol, t => t.Weight * navBefore, StringComparer.OrdinalIgnoreCase);

        double tradedValue = 0;
        foreach (string symbol in currentValue.Keys.Union(targetValue.Keys, StringComparer.OrdinalIgnoreCase))
        {
            double from = currentValue.GetValueOrDefault(symbol);
            double to = targetValue.GetValueOrDefault(symbol);
            tradedValue += Math.Abs(to - from);
        }

        double cost = tradedValue * costPerSide;
        double navAfter = navBefore - cost;

        // Re-establish the book at target weights of the post-cost NAV. Cost basis
        // for every held lot is today's price — this is the lot as of this rebalance.
        var lots = new List<ShadowLot>();
        double investedValue = 0;
        foreach (ShadowTarget t in targets.Where(t => t.Weight > 0).OrderBy(t => t.Symbol, StringComparer.Ordinal))
        {
            double price = prices[t.Symbol];
            if (price <= 0)
            {
                continue;
            }

            double value = t.Weight * navAfter;
            double shares = value / price;
            if (shares <= 0)
            {
                continue;
            }

            lots.Add(new ShadowLot(t.Symbol, t.Sleeve, shares, price));
            investedValue += shares * price;
        }

        double residualCash = navAfter - investedValue;

        return new RebalanceResult(lots, residualCash, navBefore, navAfter, tradedValue, cost);
    }
}
