using Project.Common.Domain.Abstractions;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.Abstractions.DailyRuns;

public interface IDailyRunRepository : IRepository<DailyRun>
{
    /// <summary>
    /// Latest <b>published</b> run — the § 6.2 kill switch: pending, quarantined
    /// and rolled-back runs are invisible to every serving path.
    /// </summary>
    Task<DailyRun?> GetLatestPublishedAsync(bool includePredictions = false, CancellationToken cancellationToken = default);

    Task<DailyRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyRun?> GetByGeneratedAtAsync(
        DateTime generatedAt, string market = "us", bool simulated = false,
        CancellationToken cancellationToken = default);

    /// <summary>Most recent runs regardless of status — the operator's review list.</summary>
    Task<IReadOnlyList<DailyRun>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// One prediction plus its run's timestamp, for § 6.3 audit. Deliberately not
    /// status-filtered: auditing a quarantined or rolled-back run is the point.
    /// </summary>
    Task<PredictionAudit?> GetPredictionForAuditAsync(Guid predictionId, CancellationToken cancellationToken = default);
}
