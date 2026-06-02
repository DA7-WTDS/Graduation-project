using Microsoft.EntityFrameworkCore;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Database;

namespace Project.Modules.Recommendations.Infrastructure.DailyRuns;

internal sealed class DailyRunRepository(RecommendationsDbContext dbContext) : IDailyRunRepository
{
    public async Task<DailyRun?> GetLatestAsync(bool includePredictions = false, CancellationToken cancellationToken = default)
    {
        IQueryable<DailyRun> query = dbContext.DailyRuns.AsNoTracking();

        if (includePredictions)
        {
            query = query.Include(r => r.Predictions);
        }

        return await query
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DailyRun?> GetByGeneratedAtAsync(DateTime generatedAt, CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyRuns
            .FirstOrDefaultAsync(r => r.GeneratedAt == generatedAt, cancellationToken);
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
