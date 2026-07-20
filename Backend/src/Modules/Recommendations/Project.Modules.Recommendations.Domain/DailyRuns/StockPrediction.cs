using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.DailyRuns;

/// <summary>
/// One ticker's risk-graded result within a daily run. Market-wide (no UserId);
/// personalization happens at recommendation time.
/// </summary>
public sealed class StockPrediction : Entity
{
    private StockPrediction() { }

    public Guid Id { get; private set; }
    public Guid DailyRunId { get; private set; }

    public string Ticker { get; private set; }
    public string Direction { get; private set; }          // UP / DOWN
    public double ChangePct { get; private set; }
    public double Confidence { get; private set; }
    public double SentimentScore { get; private set; }
    public string Signal { get; private set; }             // POSITIVE / NEUTRAL / NEGATIVE
    public double? AnalystRating { get; private set; }
    public string? RatingLabel { get; private set; }
    public double? PtUpsidePct { get; private set; }
    public double? NewsScore { get; private set; }
    public string Agreement { get; private set; }          // CONFIRMED / CONTRADICT / NEUTRAL
    public string RiskLevel { get; private set; }          // LOW / MEDIUM / HIGH
    public double ConvictionScore { get; private set; }
    public string[] RiskFlags { get; private set; } = [];
    public string Rationale { get; private set; }

    // Tactical dip-buyer inputs (§ 3.4): oversold state at prediction time.
    public double? Rsi14 { get; private set; }
    public double? PctVsSma50 { get; private set; }

    // Audit snapshot (§ 6.3): the exact scaled inputs this prediction was made
    // from (jsonb, opaque to the backend) and the artifacts that produced it.
    // Nullable — predictions ingested before § 6.3 have no snapshot, and a
    // pipeline that fails to emit one must never fail the ingest.
    public string? FeaturesJson { get; private set; }
    public string? ModelVersion { get; private set; }
    public string? ScalerHash { get; private set; }

    /// <summary>True when this prediction carries enough to be re-run and audited.</summary>
    public bool IsReproducible => FeaturesJson is not null;

    public static StockPrediction Create(
        string ticker,
        string direction,
        double changePct,
        double confidence,
        double sentimentScore,
        string signal,
        double? analystRating,
        string? ratingLabel,
        double? ptUpsidePct,
        double? newsScore,
        string agreement,
        string riskLevel,
        double convictionScore,
        string[] riskFlags,
        string rationale,
        double? rsi14 = null,
        double? pctVsSma50 = null,
        string? featuresJson = null,
        string? modelVersion = null,
        string? scalerHash = null)
    {
        return new StockPrediction
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Direction = direction,
            ChangePct = changePct,
            Confidence = confidence,
            SentimentScore = sentimentScore,
            Signal = signal,
            AnalystRating = analystRating,
            RatingLabel = ratingLabel,
            PtUpsidePct = ptUpsidePct,
            NewsScore = newsScore,
            Agreement = agreement,
            RiskLevel = riskLevel,
            ConvictionScore = convictionScore,
            RiskFlags = riskFlags ?? [],
            Rationale = rationale,
            Rsi14 = rsi14,
            PctVsSma50 = pctVsSma50,
            FeaturesJson = featuresJson,
            ModelVersion = modelVersion,
            ScalerHash = scalerHash
        };
    }
}
