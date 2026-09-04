namespace Project.Modules.Recommendations.Application.Recommendations.GetLatestPredictions;

/// <summary>The latest pipeline run's market-wide predictions (not personalized).</summary>
public sealed record PredictionsResponse(
    DateTime GeneratedAt,
    IReadOnlyList<PredictionItem> Predictions,
    // How to read every ChangePct below: "relative" (versus the universe median — the
    // trees champion) or "absolute" (30-day return — legacy hybrid). Clients must never
    // turn a relative score into money. See PredictionScale.
    string ScoreScale);

public sealed record PredictionItem(
    // Id makes a prediction addressable by the § 6.3 reproduce endpoint —
    // without it an audit trail exists but cannot be walked.
    Guid Id,
    string Ticker,
    string Direction,
    double ChangePct,
    double Confidence,
    string Signal,
    string RiskLevel,
    double ConvictionScore,
    string Rationale);
