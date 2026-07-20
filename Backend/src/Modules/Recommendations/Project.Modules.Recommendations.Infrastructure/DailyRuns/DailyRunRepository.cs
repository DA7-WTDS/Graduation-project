using Microsoft.EntityFrameworkCore;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Database;

namespace Project.Modules.Recommendations.Infrastructure.DailyRuns;

internal sealed class DailyRunRepository(RecommendationsDbContext dbContext) : IDailyRunRepository
{
    public async Task<DailyRun?> GetLatestPublishedAsync(bool includePredictions = false, CancellationToken cancellationToken = default)
    {
        IQueryable<DailyRun> query = dbContext.DailyRuns.AsNoTracking();

        if (includePredictions)
        {
            query = query.Include(r => r.Predictions);
        }

        // The one WHERE clause that makes the whole system kill-switch-aware (§ 6.2).
        return await query
            .Where(r => r.Status == DailyRunStatus.Published)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DailyRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyRuns
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<DailyRun?> GetByGeneratedAtAsync(DateTime generatedAt, CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyRuns
            .FirstOrDefaultAsync(r => r.GeneratedAt == generatedAt, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyRun>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyRuns
            .AsNoTracking()
            .OrderByDescending(r => r.GeneratedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<PredictionAudit?> GetPredictionForAuditAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        return await (
            from p in dbContext.StockPredictions.AsNoTracking()
            join r in dbContext.DailyRuns.AsNoTracking() on p.DailyRunId equals r.Id
            where p.Id == predictionId
            select new PredictionAudit(p, r.GeneratedAt, r.Status.ToString()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DailyRun> AddAsync(DailyRun entity, CancellationToken cancellationToken = default)
    {
        var result = await dbContext.DailyRuns.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public void Update(DailyRun entity, CancellationToken cancellationToken = default)
    {
        dbContext.DailyRuns.Update(entity);
    }

    public void Delete(DailyRun entity, CancellationToken cancellationToken = default)
    {
        dbContext.DailyRuns.Remove(entity);
    }
}
