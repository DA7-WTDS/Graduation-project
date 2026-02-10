using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.PublicApi;

namespace Project.Modules.Portfolio.Infrastructure.PublicApi;

internal sealed class PortfolioApi(PortfolioDbContext dbContext) : IPortfolioApi
{
    public async Task<PortfolioResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var portfolio = await dbContext.Portfolios
            .AsNoTracking()
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
}
