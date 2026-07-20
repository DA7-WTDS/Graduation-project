using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.Abstractions.DailyRuns;

/// <summary>A stored prediction together with the run it belonged to (§ 6.3).</summary>
public sealed record PredictionAudit(StockPrediction Prediction, DateTime RunGeneratedAt, string RunStatus);
