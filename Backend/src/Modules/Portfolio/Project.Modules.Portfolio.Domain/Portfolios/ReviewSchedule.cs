namespace Project.Modules.Portfolio.Domain.Portfolios;

/// <summary>
/// When a portfolio is next due for review, from its template's rebalance
/// cadence (§ 3.2). Pure: the next occurrence strictly after <paramref name="now"/>,
/// counted from inception — a set-and-forget investor is not asked to look at
/// their plan more often than their template says.
/// </summary>
public static class ReviewSchedule
{
    public static DateTime NextReview(DateTime inception, string cadence, DateTime now)
    {
        Func<DateTime, DateTime> step = cadence?.ToLowerInvariant() switch
        {
            "weekly" => d => d.AddDays(7),
            "monthly" => d => d.AddMonths(1),
            "quarterly" => d => d.AddMonths(3),
            "semi_annual" => d => d.AddMonths(6),
            "annual" => d => d.AddYears(1),
            _ => d => d.AddMonths(1),
        };

        DateTime next = step(inception);

        // Walk forward past any occurrences already behind us (a portfolio held
        // through several cadences still gets a future date, not a stale one).
        for (int guard = 0; next <= now && guard < 1000; guard++)
        {
            next = step(next);
        }

        return next;
    }
}
