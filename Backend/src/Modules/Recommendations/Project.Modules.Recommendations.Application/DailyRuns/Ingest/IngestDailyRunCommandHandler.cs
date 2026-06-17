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

        // The pipeline sends generated_at with an offset (e.g. +00:00), which
        // deserializes as DateTimeKind.Local. PostgreSQL 'timestamp with time zone'
        // requires UTC, so normalize before querying/persisting.
        DateTime generatedAtUtc = request.GeneratedAt.Kind switch
        {
            DateTimeKind.Utc => request.GeneratedAt,
            DateTimeKind.Local => request.GeneratedAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(request.GeneratedAt, DateTimeKind.Utc),
        };

        // Idempotent: a retried run with the same timestamp returns the existing run.
        DailyRun? existing = await dailyRunRepository.GetByGeneratedAtAsync(generatedAtUtc, cancellationToken);
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

        DailyRun run = DailyRun.Create(generatedAtUtc, predictions);

        await dailyRunRepository.AddAsync(run, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(run.Id);
    }
}
