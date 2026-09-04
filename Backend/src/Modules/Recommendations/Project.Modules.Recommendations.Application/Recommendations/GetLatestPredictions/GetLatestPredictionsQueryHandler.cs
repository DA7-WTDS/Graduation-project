using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Domain.DailyRuns;
using static Project.Modules.Recommendations.Domain.DailyRuns.RecommendationErrors;

namespace Project.Modules.Recommendations.Application.Recommendations.GetLatestPredictions;

internal sealed class GetLatestPredictionsQueryHandler(IDailyRunRepository dailyRunRepository)
    : IQueryHandler<GetLatestPredictionsQuery, PredictionsResponse>
{
    public async Task<Result<PredictionsResponse>> Handle(GetLatestPredictionsQuery request, CancellationToken cancellationToken)
    {
        DailyRun? run = await dailyRunRepository.GetLatestPublishedAsync(includePredictions: true, cancellationToken);
        if (run is null || run.Predictions.Count == 0)
        {
            return Result.Fail(NoRunAvailable);
        }

        var predictions = run.Predictions
            .OrderByDescending(p => p.ConvictionScore)
            .Select(p => new PredictionItem(
                p.Id,
                p.Ticker,
                p.Direction,
                p.ChangePct,
                p.Confidence,
                p.Signal,
                p.RiskLevel,
                p.ConvictionScore,
                p.Rationale))
            .ToList();

        return Result.Ok(new PredictionsResponse(
            run.GeneratedAt, predictions, PredictionScale.Of(run.Predictions)));
    }
}
