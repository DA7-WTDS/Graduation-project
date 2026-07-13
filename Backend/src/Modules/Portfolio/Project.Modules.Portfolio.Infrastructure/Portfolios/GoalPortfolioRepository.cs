using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

internal sealed class GoalPortfolioRepository(PortfolioDbContext dbContext) : IGoalPortfolioRepository
{
    public async Task<GoalPortfolio?> GetActiveByGoalIdAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GoalPortfolios
            .Include(p => p.Holdings)
            .FirstOrDefaultAsync(p => p.GoalId == goalId && p.Status == GoalPortfolioStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<GoalPortfolio>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.GoalPortfolios
            .Include(p => p.Holdings)
            .Where(p => p.Status == GoalPortfolioStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GoalPortfolio portfolio, CancellationToken cancellationToken = default)
    {
        await dbContext.GoalPortfolios.AddAsync(portfolio, cancellationToken);
    }
}
