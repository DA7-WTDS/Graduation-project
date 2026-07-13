namespace Project.Modules.Portfolio.Domain.Portfolios;

/// <summary>Pure valuation math for the monitoring engine (§ 3.5). Kept separate
/// from the aggregate so the arithmetic is trivially unit-testable.</summary>
public static class PortfolioValuation
{
    /// <summary>Mark-to-market NAV: Σ shares × current price. Positions whose
    /// price is unknown tonight are excluded by the caller, not valued at zero.</summary>
    public static double Nav(IEnumerable<(double Shares, double Price)> positions) =>
        positions.Sum(p => p.Shares * p.Price);

    public static double Drawdown(double nav, double highWaterMark) =>
        highWaterMark <= 0 ? 0 : Math.Max(0, 1 - nav / highWaterMark);

    /// <summary>Largest absolute gap between any position's current weight and
    /// its target — the "how far out of balance are we" number (in weight units,
    /// e.g. 0.12 = 12 percentage points).</summary>
    public static double MaxDrift(
        IEnumerable<(string Symbol, double CurrentValue, double TargetWeight)> positions)
    {
        List<(string Symbol, double CurrentValue, double TargetWeight)> list = positions.ToList();
        double nav = list.Sum(p => p.CurrentValue);
        if (nav <= 0)
        {
            return 0;
        }

        return list.Max(p => Math.Abs(p.CurrentValue / nav - p.TargetWeight));
    }
}
