namespace Project.Modules.Portfolio.Domain.Shadow;

/// <summary>Summary stats for one shadow portfolio's NAV series (§ 6.1). Pure so
/// the performance math is unit-checkable — the numbers are public-facing.</summary>
public static class ShadowPerformance
{
    public sealed record Summary(
        double TotalReturn,
        double AnnualizedReturn,
        double MaxDrawdown,
        int Days);

    /// <summary>NAV points must be in date order, oldest first.</summary>
    public static Summary Compute(IReadOnlyList<double> navSeries, decimal notional)
    {
        double start = (double)notional;
        if (navSeries.Count == 0 || start <= 0)
        {
            return new Summary(0, 0, 0, navSeries.Count);
        }

        double last = navSeries[^1];
        double totalReturn = last / start - 1;

        // Peak-to-trough over the whole series, seeded at the starting notional
        // so a drop before the first snapshot is still captured.
        double peak = start;
        double maxDrawdown = 0;
        foreach (double nav in navSeries)
        {
            if (nav > peak)
            {
                peak = nav;
            }
            if (peak > 0)
            {
                maxDrawdown = Math.Max(maxDrawdown, 1 - nav / peak);
            }
        }

        // Annualize on ~252 trading days; below a year, report the raw return
        // rather than extrapolating a handful of days into a misleading CAGR.
        int days = navSeries.Count;
        double annualized = days >= 252
            ? Math.Pow(1 + totalReturn, 252.0 / days) - 1
            : totalReturn;

        return new Summary(totalReturn, annualized, maxDrawdown, days);
    }
}
