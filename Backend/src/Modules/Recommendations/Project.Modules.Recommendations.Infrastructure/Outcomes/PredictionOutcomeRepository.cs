using Microsoft.EntityFrameworkCore;
using Project.Modules.Recommendations.Application.Abstractions.Outcomes;
using Project.Modules.Recommendations.Infrastructure.Database;

namespace Project.Modules.Recommendations.Infrastructure.Outcomes;

internal sealed class PredictionOutcomeRepository(RecommendationsDbContext dbContext)
    : IPredictionOutcomeRepository
{
    public async Task<IReadOnlyList<OutcomeStat>> GetSinceAsync(
        DateTime since, CancellationToken cancellationToken = default)
    {
        return await dbContext.PredictionOutcomes
            .AsNoTracking()
            .Where(o => o.RunGeneratedAt >= since)
            .Select(o => new OutcomeStat(o.RunGeneratedAt, o.RiskLevel, o.DirectionHit, o.RealizedReturnPct))
            .ToListAsync(cancellationToken);
    }
}
