using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.Outcomes;

/// <summary>
/// The realized result of one <c>StockPrediction</c> after its horizon elapsed —
/// the feedback loop of IMPLEMENTATION_PLAN § 0.3. Written once by the nightly
/// ScoreOutcomesJob; immutable afterwards. Feeds rolling model metrics and the
/// public track-record page.
/// </summary>
public sealed class PredictionOutcome : Entity
{
    private PredictionOutcome() { }

    public Guid Id { get; private set; }

    /// <summary>The prediction this outcome scores (unique — one outcome per prediction).</summary>
    public Guid StockPredictionId { get; private set; }

    // Denormalized from the prediction/run so metrics queries never need joins.
    public string Ticker { get; private set; } = string.Empty;
    public DateTime RunGeneratedAt { get; private set; }
    public string PredictedDirection { get; private set; } = string.Empty; // UP / DOWN
    public double PredictedChangePct { get; private set; }
    public string RiskLevel { get; private set; } = string.Empty;          // LOW / MEDIUM / HIGH

    public int HorizonDays { get; private set; }

    /// <summary>Close on the last trading day at/before the run date (the entry price).</summary>
    public double BaselineClose { get; private set; }

    /// <summary>Close on the first trading day at/after run date + horizon.</summary>
    public double RealizedClose { get; private set; }

    public double RealizedReturnPct { get; private set; }
    public bool DirectionHit { get; private set; }
    public DateTime ScoredAt { get; private set; }

    public static PredictionOutcome Create(
        Guid stockPredictionId,
        string ticker,
        DateTime runGeneratedAt,
        string predictedDirection,
        double predictedChangePct,
        string riskLevel,
        int horizonDays,
        double baselineClose,
        double realizedClose)
    {
        double realizedReturnPct = (realizedClose - baselineClose) / baselineClose * 100.0;

        return new PredictionOutcome
        {
            Id = Guid.NewGuid(),
            StockPredictionId = stockPredictionId,
            Ticker = ticker,
            RunGeneratedAt = runGeneratedAt,
            PredictedDirection = predictedDirection,
            PredictedChangePct = predictedChangePct,
            RiskLevel = riskLevel,
            HorizonDays = horizonDays,
            BaselineClose = baselineClose,
            RealizedClose = realizedClose,
            RealizedReturnPct = Math.Round(realizedReturnPct, 4),
            DirectionHit = (predictedDirection == "UP") == (realizedReturnPct > 0),
            ScoredAt = DateTime.UtcNow
        };
    }
}
