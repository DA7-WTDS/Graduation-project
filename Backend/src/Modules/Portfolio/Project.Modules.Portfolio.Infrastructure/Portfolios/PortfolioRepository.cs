
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

internal sealed class PortfolioRepository(PortfolioDbContext dbContext) : IPortfolioRepository
{
    public async Task<Domain.Portfolios.Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Portfolios.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Domain.Portfolios.Portfolio?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Portfolios.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<Domain.Portfolios.Portfolio> AddAsync(Domain.Portfolios.Portfolio entity, CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Portfolios.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public void Update(Domain.Portfolios.Portfolio entity, CancellationToken cancellationToken = default)
    {
        dbContext.Portfolios.Update(entity);
    }

    public void Delete(Domain.Portfolios.Portfolio entity, CancellationToken cancellationToken = default)
    {
        dbContext.Portfolios.Remove(entity);
    }
}
