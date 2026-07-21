using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;
using Project.Modules.Portfolio.Domain.Shadow;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

internal sealed class ShadowPortfolioRepository(PortfolioDbContext dbContext) : IShadowPortfolioRepository
{
    public async Task<IReadOnlyList<ShadowPortfolio>> GetAllForMarketAsync(string market, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShadowPortfolios
            .Include(p => p.Positions)
            .Where(p => p.Market == market)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShadowPortfolio portfolio, CancellationToken cancellationToken = default)
    {
        await dbContext.ShadowPortfolios.AddAsync(portfolio, cancellationToken);
    }

    public async Task AddSnapshotAsync(ShadowSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await dbContext.ShadowSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public async Task<bool> SnapshotExistsAsync(Guid portfolioId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShadowSnapshots
            .AnyAsync(s => s.ShadowPortfolioId == portfolioId && s.Date == date, cancellationToken);
    }

    public async Task<IReadOnlyList<ShadowSnapshot>> GetAllSnapshotsAsync(string market, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShadowSnapshots
            .AsNoTracking()
            .Where(s => dbContext.ShadowPortfolios.Any(p => p.Id == s.ShadowPortfolioId && p.Market == market))
            .OrderBy(s => s.Date)
            .ToListAsync(cancellationToken);
    }
}
