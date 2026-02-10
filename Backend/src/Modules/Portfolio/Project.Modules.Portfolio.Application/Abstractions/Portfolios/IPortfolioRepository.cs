
using Project.Common.Domain.Abstractions;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Application.Abstractions.Portfolios;

public interface IPortfolioRepository : IRepository<Domain.Portfolios.Portfolio>
{
    Task<Domain.Portfolios.Portfolio?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
