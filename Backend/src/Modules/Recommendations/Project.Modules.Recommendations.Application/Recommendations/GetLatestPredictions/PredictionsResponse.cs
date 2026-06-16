namespace Project.Modules.Recommendations.Application.Recommendations.GetLatestPredictions;

/// <summary>The latest pipeline run's market-wide predictions (not personalized).</summary>
public sealed record PredictionsResponse(
    DateTime GeneratedAt,
    IReadOnlyList<PredictionItem> Predictions);

public sealed record PredictionItem(
    string Ticker,
    string Direction,
    double ChangePct,
    double Confidence,
    string Signal,
    string RiskLevel,
    double ConvictionScore,
    string Rationale);
