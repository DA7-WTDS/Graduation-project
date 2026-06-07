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
}
