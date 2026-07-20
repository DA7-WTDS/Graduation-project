using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.DailyRuns.Reproduce;

/// <summary>
/// § 6.3 audit: re-run a stored prediction from its captured feature vector and
/// report whether today's artifacts still produce what was served.
/// </summary>
public sealed record ReproducePredictionQuery(Guid PredictionId) : IQuery<ReproducePredictionResponse>;

public sealed record ReproducePredictionResponse(
    Guid PredictionId,
    string Ticker,
    DateTime RunGeneratedAt,
    StoredPrediction Stored,
    RecomputedPrediction Recomputed,
    bool Matches,
    IReadOnlyList<string> Mismatches,
    bool ModelVersionMatches,
    bool ScalerHashMatches);

public sealed record StoredPrediction(
    string Direction,
    double ChangePct,
    double Confidence,
    string? ModelVersion,
    string? ScalerHash);

public sealed record RecomputedPrediction(
    string Direction,
    double ChangePct,
    double Confidence,
    string ModelVersion,
    string ScalerHash);
