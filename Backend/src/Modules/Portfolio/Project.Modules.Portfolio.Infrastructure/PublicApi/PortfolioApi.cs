using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.PublicApi;

namespace Project.Modules.Portfolio.Infrastructure.PublicApi;

internal sealed class PortfolioApi(PortfolioDbContext dbContext) : IPortfolioApi
{
    public async Task<PortfolioResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var portfolio = await dbContext.Portfolios
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (portfolio == null)
            return null;

        return new PortfolioResponse(
            portfolio.Id,
            portfolio.UserId,
            portfolio.RiskProfile.ToString(),
            portfolio.StocksPercentage,
            portfolio.BondsPercentage,
            portfolio.EtfsPercentage,
            portfolio.CashPercentage
        );
    }

    public async Task<IReadOnlyList<PortfolioResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var portfolios = await dbContext.Portfolios
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return portfolios
            .Select(p => new PortfolioResponse(
                p.Id,
                p.UserId,
                p.RiskProfile.ToString(),
                p.StocksPercentage,
                p.BondsPercentage,
                p.EtfsPercentage,
                p.CashPercentage))
            .ToList();
    }
}
