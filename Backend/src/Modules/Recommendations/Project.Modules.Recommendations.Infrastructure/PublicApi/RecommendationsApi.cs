using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Recommendations.PublicApi;

namespace Project.Modules.Recommendations.Infrastructure.PublicApi;

internal sealed class RecommendationsApi(RecommendationsDbContext dbContext) : IRecommendationsApi
{
    public async Task<Guid?> GetLatestDailyRunIdAsync(CancellationToken cancellationToken = default)
    {
        var latestRun = await dbContext.DailyRuns
            .AsNoTracking()
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return latestRun;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<RankedTicker>> GetLatestRankedTickersAsync(
        CancellationToken cancellationToken = default)
    {
        Guid? runId = await GetLatestDailyRunIdAsync(cancellationToken);
        if (runId is null)
        {
            return [];
        }

        return await dbContext.StockPredictions
            .AsNoTracking()
            .Where(p => p.DailyRunId == runId)
            .OrderByDescending(p => p.ConvictionScore)
            .ThenBy(p => p.Ticker)
            .Select(p => new RankedTicker(p.Ticker, p.Direction, p.RiskLevel, p.ConvictionScore, p.ChangePct))
            .ToListAsync(cancellationToken);
    }
}
