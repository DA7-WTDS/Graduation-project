using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Domain.DailyRuns;
using static Project.Modules.Recommendations.Domain.DailyRuns.RecommendationErrors;

namespace Project.Modules.Recommendations.Application.DailyRuns.Ingest;

internal sealed class IngestDailyRunCommandHandler(
    IDailyRunRepository dailyRunRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<IngestDailyRunCommand, Guid>
{
    public async Task<Result<Guid>> Handle(IngestDailyRunCommand request, CancellationToken cancellationToken)
    {
        if (request.Records is null || request.Records.Count == 0)
        {
            return Result.Fail(InvalidIngestPayload("no records provided"));
        }

        // Idempotent: a retried run with the same timestamp returns the existing run.
        DailyRun? existing = await dailyRunRepository.GetByGeneratedAtAsync(request.GeneratedAt, cancellationToken);
        if (existing is not null)
        {
            return Result.Ok(existing.Id);
        }

        IEnumerable<StockPrediction> predictions = request.Records.Select(r => StockPrediction.Create(
            r.Ticker,
            r.Direction,
            r.ChangePct,
            r.Confidence,
            r.SentimentScore,
            r.Signal,
            r.AnalystRating,
            r.RatingLabel,
            r.PtUpsidePct,
            r.NewsScore,
            r.Agreement,
            r.RiskLevel,
            r.ConvictionScore,
            r.RiskFlags,
            r.Rationale));

        DailyRun run = DailyRun.Create(request.GeneratedAt, predictions);

        await dailyRunRepository.AddAsync(run, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(run.Id);
    }
}
