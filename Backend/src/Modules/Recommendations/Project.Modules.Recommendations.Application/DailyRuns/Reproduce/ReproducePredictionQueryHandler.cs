using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Abstractions.Pipeline;
using Project.Modules.Recommendations.Domain.DailyRuns;
using static Project.Modules.Recommendations.Domain.DailyRuns.RecommendationErrors;

namespace Project.Modules.Recommendations.Application.DailyRuns.Reproduce;

internal sealed class ReproducePredictionQueryHandler(
    IDailyRunRepository dailyRunRepository,
    IPipelineReproducer reproducer)
    : IQueryHandler<ReproducePredictionQuery, ReproducePredictionResponse>
{
    // The pipeline rounds change_pct/confidence to 4dp, so anything at or below
    // half a unit in the last place is representation noise, not a real change.
    private const double Tolerance = 1e-4;

    public async Task<Result<ReproducePredictionResponse>> Handle(
        ReproducePredictionQuery request, CancellationToken cancellationToken)
    {
        PredictionAudit? audit = await dailyRunRepository.GetPredictionForAuditAsync(
            request.PredictionId, cancellationToken);

        if (audit is null)
        {
            return Result.Fail(PredictionNotFound(request.PredictionId));
        }

        StockPrediction p = audit.Prediction;
        if (!p.IsReproducible)
        {
            // Predictions made before § 6.3 have no snapshot and never will —
            // say so plainly rather than implying the audit failed.
            return Result.Fail(PredictionNotReproducible(request.PredictionId));
        }

        ReproduceResult recomputed = await reproducer.ReproduceAsync(
            p.FeaturesJson!, p.ModelVersion, p.ScalerHash, cancellationToken);

        var mismatches = new List<string>();
        if (!string.Equals(p.Direction, recomputed.Direction, StringComparison.OrdinalIgnoreCase))
        {
            mismatches.Add($"direction: stored {p.Direction}, recomputed {recomputed.Direction}");
        }
        if (Math.Abs(p.ChangePct - recomputed.ChangePct) > Tolerance)
        {
            mismatches.Add($"changePct: stored {p.ChangePct}, recomputed {recomputed.ChangePct}");
        }
        if (Math.Abs(p.Confidence - recomputed.Confidence) > Tolerance)
        {
            mismatches.Add($"confidence: stored {p.Confidence}, recomputed {recomputed.Confidence}");
        }

        // Artifact drift is reported, not treated as failure: reproducing an old
        // prediction under new artifacts is how you show what a model change did.
        bool modelMatches = p.ModelVersion is not null && p.ModelVersion == recomputed.ModelVersion;
        bool scalerMatches = p.ScalerHash is not null && p.ScalerHash == recomputed.ScalerHash;

        return Result.Ok(new ReproducePredictionResponse(
            p.Id,
            p.Ticker,
            audit.RunGeneratedAt,
            new StoredPrediction(p.Direction, p.ChangePct, p.Confidence, p.ModelVersion, p.ScalerHash),
            new RecomputedPrediction(
                recomputed.Direction, recomputed.ChangePct, recomputed.Confidence,
                recomputed.ModelVersion, recomputed.ScalerHash),
            mismatches.Count == 0,
            mismatches,
            modelMatches,
            scalerMatches));
    }
}
