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
        string rationale)
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
            Rationale = rationale
        };
    }
}
