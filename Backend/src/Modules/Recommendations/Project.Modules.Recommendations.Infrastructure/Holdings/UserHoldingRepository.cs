using Microsoft.EntityFrameworkCore;
using Project.Modules.Recommendations.Application.Abstractions.Holdings;
using Project.Modules.Recommendations.Domain.Holdings;
using Project.Modules.Recommendations.Infrastructure.Database;

namespace Project.Modules.Recommendations.Infrastructure.Holdings;

internal sealed class UserHoldingRepository(RecommendationsDbContext dbContext) : IUserHoldingRepository
{
    public async Task<IReadOnlyList<UserHolding>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserHoldings
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForUserAsync(Guid userId, IEnumerable<UserHolding> holdings, CancellationToken cancellationToken = default)
    {
        List<UserHolding> existing = await dbContext.UserHoldings
            .Where(h => h.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.UserHoldings.RemoveRange(existing);
        await dbContext.UserHoldings.AddRangeAsync(holdings, cancellationToken);
    }
}
