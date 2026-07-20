using FluentResults;
using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.DailyRuns;

/// <summary>
/// A single daily pipeline run (the ~100-stock market-wide batch), aggregate root
/// over its <see cref="StockPrediction"/> children. Ingested from the pipeline.
///
/// Carries the § 6.2 kill-switch lifecycle: a run lands as Published,
/// PendingReview (manual approval mode) or Quarantined (failed quality gates),
/// and only Published runs are served. Every transition is operator- or
/// gate-driven and recorded with a reason.
/// </summary>
public sealed class DailyRun : Entity
{
    private readonly List<StockPrediction> _predictions = [];

    private DailyRun() { }

    public Guid Id { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public int Count { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DailyRunStatus Status { get; private set; }
    public string? StatusReason { get; private set; }
    public DateTime StatusChangedAt { get; private set; }

    public IReadOnlyCollection<StockPrediction> Predictions => _predictions.AsReadOnly();

    public static DailyRun Create(
        DateTime generatedAt,
        IEnumerable<StockPrediction> predictions,
        DailyRunStatus status = DailyRunStatus.Published,
        string? statusReason = null)
    {
        if (status == DailyRunStatus.RolledBack)
        {
            throw new InvalidOperationException("A run cannot be ingested as RolledBack.");
        }

        var run = new DailyRun
        {
            Id = Guid.NewGuid(),
            GeneratedAt = generatedAt,
            CreatedAt = DateTime.UtcNow,
            Status = status,
            StatusReason = statusReason,
            StatusChangedAt = DateTime.UtcNow,
        };

        run._predictions.AddRange(predictions);
        run.Count = run._predictions.Count;

        run.Raise(new DailyRunIngestedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow, run.Id, run.GeneratedAt, status.ToString(), statusReason));

        if (status == DailyRunStatus.Published)
        {
            run.Raise(new DailyRunPublishedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, run.Id, run.GeneratedAt));
        }

        return run;
    }

    /// <summary>
    /// Operator kill switch: flip the run's lifecycle state. Valid transitions:
    /// PendingReview → Published/Quarantined (approve or reject),
    /// Quarantined → Published (gate false-positive override),
    /// Published → RolledBack (pull a bad run), RolledBack → Published (undo).
    /// </summary>
    public Result ChangeStatus(DailyRunStatus target, string? reason = null)
    {
        bool allowed = (Status, target) switch
        {
            (DailyRunStatus.PendingReview, DailyRunStatus.Published) => true,
            (DailyRunStatus.PendingReview, DailyRunStatus.Quarantined) => true,
            (DailyRunStatus.Quarantined, DailyRunStatus.Published) => true,
            (DailyRunStatus.Published, DailyRunStatus.RolledBack) => true,
            (DailyRunStatus.RolledBack, DailyRunStatus.Published) => true,
            _ => false,
        };

        if (!allowed)
        {
            return Result.Fail(RecommendationErrors.InvalidStatusTransition(Status, target));
        }

        Status = target;
        StatusReason = reason;
        StatusChangedAt = DateTime.UtcNow;

        if (target == DailyRunStatus.Published)
        {
            Raise(new DailyRunPublishedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, Id, GeneratedAt));
        }

        return Result.Ok();
    }
}
