namespace Project.Modules.Recommendations.Domain.Monitoring;

/// <summary>
/// Pure trigger rules for the monitoring engine (§ 3.5). Stateless by design:
/// "fire on crossing" is computed from the data itself (today's window vs
/// yesterday's), so a persisting condition alerts once, not every night.
/// </summary>
public static class MonitorRules
{
    /// <summary>
    /// Market-crash trigger: the index's trailing <paramref name="windowDays"/>
    /// trading-day return crossed below −<paramref name="dropPct"/> today.
    /// <paramref name="closes"/> must be ordered oldest→newest.
    /// </summary>
    public static (bool CrossedToday, double CurrentDrop) CrashCrossed(
        IReadOnlyList<double> closes, int windowDays, double dropPct)
    {
        // Need today's window AND yesterday's window to detect the crossing.
        if (closes.Count < windowDays + 2)
        {
            return (false, 0);
        }

        double dropAt(int last) => closes[last] / closes[last - windowDays] - 1.0;

        double today = dropAt(closes.Count - 1);
        double yesterday = dropAt(closes.Count - 2);

        return (today <= -dropPct && yesterday > -dropPct, today);
    }

    /// <summary>A held name has "flipped" when the latest run says DOWN with
    /// NEGATIVE sentiment — the model and the news agree against the position.</summary>
    public static bool IsFlipped(string? direction, string? signal) =>
        string.Equals(direction, "DOWN", StringComparison.OrdinalIgnoreCase)
        && string.Equals(signal, "NEGATIVE", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Conviction-reversal trigger: held tickers that are flipped in the latest
    /// run but were NOT flipped in the previous one (alert once per flip, not
    /// every night it stays bearish). A ticker absent from the previous run
    /// counts as not-previously-flipped.
    /// </summary>
    public static IReadOnlyList<string> NewReversals(
        IEnumerable<string> heldTickers,
        IReadOnlyDictionary<string, (string Direction, string Signal)> latestRun,
        IReadOnlyDictionary<string, (string Direction, string Signal)> previousRun)
    {
        var reversals = new List<string>();
        foreach (string ticker in heldTickers.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.Ordinal))
        {
            if (!latestRun.TryGetValue(ticker, out (string Direction, string Signal) latest) || !IsFlipped(latest.Direction, latest.Signal))
            {
                continue;
            }

            bool wasFlipped = previousRun.TryGetValue(ticker, out (string Direction, string Signal) prev)
                && IsFlipped(prev.Direction, prev.Signal);
            if (!wasFlipped)
            {
                reversals.Add(ticker);
            }
        }

        return reversals;
    }

    /// <summary>
    /// Model-drift trigger (§ 1.7): the rolling directional hit-rate crossed below
    /// <paramref name="threshold"/> tonight.
    ///
    /// Same "fire on crossing" discipline as the crash rule, and for the same reason: the
    /// nightly job re-evaluates a slow-moving window, so a level test alone would alert
    /// every night for as long as the model stayed bad. <paramref name="previousHitRate"/>
    /// is the same statistic as it stood before tonight's outcomes were scored.
    ///
    /// A null previous rate means the window had too few samples to judge yesterday. That
    /// is treated as "not previously alarming", so the first night the metric becomes
    /// measurable AND bad does alert — which is the night someone needs to know.
    /// </summary>
    public static bool DriftCrossed(double hitRate, double? previousHitRate, double threshold) =>
        hitRate < threshold && (previousHitRate is null || previousHitRate >= threshold);

}
