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

    public async Task<MonitoringProfileResponse?> GetMonitoringProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var portfolio = await dbContext.Portfolios
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.RiskProfile })
            .FirstOrDefaultAsync(cancellationToken);

        if (portfolio is null)
        {
            return null;
        }

        // Latest investor profile across the user's goals (v1 UI is single-goal,
        // but take the newest either way). Null for pre-Phase-2 legacy users.
        var profile = await (
            from g in dbContext.Goals
            join ip in dbContext.InvestorProfiles on g.Id equals ip.GoalId
            where g.UserId == userId
            orderby ip.CreatedAt descending
            select new { GoalType = g.Type, ip.Engagement })
            .FirstOrDefaultAsync(cancellationToken);

        return new MonitoringProfileResponse(
            userId,
            portfolio.RiskProfile.ToString(),
            profile?.GoalType.ToString(),
            profile?.Engagement.ToString());
    }
}
