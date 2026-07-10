namespace Project.Modules.Recommendations.Application.Recommendations.GetTrackRecord;

/// <summary>
/// Rolling realized-outcome metrics (IMPLEMENTATION_PLAN § 0.3) — the honest
/// answer to "how right were the predictions?". Direction hit-rate is measured
/// against realized 30-day returns; base rate context matters (an always-up
/// predictor is the fair comparison, not 50%).
/// </summary>
public sealed record TrackRecordResponse(
    int TotalScored,
    DateTime? FirstRunAt,
    DateTime? LastRunAt,
    IReadOnlyList<WindowStats> Windows);

/// <summary>Metrics over runs generated in the trailing window.</summary>
public sealed record WindowStats(
    int WindowDays,
    int Count,
    double HitRatePct,
    double AvgRealizedReturnPct,
    IReadOnlyList<RiskBucketStats> ByRiskLevel);

public sealed record RiskBucketStats(
    string RiskLevel,
    int Count,
    double HitRatePct,
    double AvgRealizedReturnPct);
