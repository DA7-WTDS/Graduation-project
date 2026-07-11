using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Project.Modules.Recommendations.PublicApi;

/// <summary>One ticker from the latest daily run, as consumed by the allocation
/// optimizer (§ 3.3 core sleeve, § 3.4 tactical sleeve). ConvictionScore blends
/// model, sentiment, and analyst agreement; RiskLevel is the pipeline's risk
/// grade (LOW/MEDIUM/HIGH); Rsi14/PctVsSma50 describe the oversold state and
/// Signal ("POSITIVE"/"NEUTRAL"/"NEGATIVE") the sentiment direction.</summary>
public sealed record RankedTicker(
    string Ticker,
    string Direction,
    string RiskLevel,
    double ConvictionScore,
    double ChangePct,
    double SentimentScore,
    string Signal,
    double? Rsi14,
    double? PctVsSma50);

public interface IRecommendationsApi
{
    Task<Guid?> GetLatestDailyRunIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Latest run's predictions ordered by conviction (best first);
    /// empty when no run has been ingested yet.</summary>
    Task<IReadOnlyList<RankedTicker>> GetLatestRankedTickersAsync(CancellationToken cancellationToken = default);
}
