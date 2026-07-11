using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Application.Abstractions.Strategies;
using Project.Modules.Portfolio.Domain.Strategies;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Strategies;

internal sealed class StrategyTemplateRepository(PortfolioDbContext dbContext) : IStrategyTemplateRepository
{
    public async Task<IReadOnlyList<StrategyTemplate>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.StrategyTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Key)
            .ToListAsync(cancellationToken);
    }
}
