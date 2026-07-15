namespace Project.Modules.Portfolio.Domain.Portfolios;

/// <summary>
/// How often a user hears from us with a portfolio summary (§ 3.5, last row).
/// Paced by the engagement answer they gave in the questionnaire — a
/// set-and-forget investor gets a quarterly note, not a monthly nudge. This is
/// the promise that answering "only tell me when it matters" actually means
/// something.
/// </summary>
public static class DigestSchedule
{
    public static int CadenceDays(string? engagement) => engagement?.ToLowerInvariant() switch
    {
        "setandforget" => 90,   // quarterly
        "monthly" => 30,
        "daily" => 30,          // they already get daily signals; the digest is the periodic step back
        _ => 30,
    };

    /// <summary>Due one cadence after the last digest — or after inception if we
    /// have never sent one (a brand-new portfolio isn't summarized on day one).</summary>
    public static bool IsDue(string? engagement, DateTime inception, DateTime? lastDigestAt, DateTime now) =>
        now >= (lastDigestAt ?? inception).AddDays(CadenceDays(engagement));
}
