using System.Text.Json.Serialization;

namespace Project.Modules.Recommendations.Application.DailyRuns.Ingest;

/// <summary>
/// One record in the pipeline's ingest payload. JSON keys are snake_case to match
/// the n8n risk-node output (ticker, change_pct, risk_level, ...).
/// </summary>
public sealed record PredictionRecordDto
{
    [JsonPropertyName("ticker")]           public string Ticker { get; init; } = string.Empty;
    [JsonPropertyName("direction")]        public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("change_pct")]       public double ChangePct { get; init; }
    [JsonPropertyName("confidence")]       public double Confidence { get; init; }
    [JsonPropertyName("sentiment_score")]  public double SentimentScore { get; init; }
    [JsonPropertyName("signal")]           public string Signal { get; init; } = string.Empty;
    [JsonPropertyName("analyst_rating")]   public double? AnalystRating { get; init; }
    [JsonPropertyName("rating_label")]     public string? RatingLabel { get; init; }
    [JsonPropertyName("pt_upside_pct")]    public double? PtUpsidePct { get; init; }
    [JsonPropertyName("news_score")]       public double? NewsScore { get; init; }
    [JsonPropertyName("agreement")]        public string Agreement { get; init; } = string.Empty;
    [JsonPropertyName("risk_level")]       public string RiskLevel { get; init; } = string.Empty;
    [JsonPropertyName("conviction_score")] public double ConvictionScore { get; init; }
    [JsonPropertyName("risk_flags")]       public string[] RiskFlags { get; init; } = [];
    [JsonPropertyName("rationale")]        public string Rationale { get; init; } = string.Empty;
}
