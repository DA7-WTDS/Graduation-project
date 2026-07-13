using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Application.Abstractions.Portfolios;

public interface IGoalPortfolioRepository
{
    Task<GoalPortfolio?> GetActiveByGoalIdAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>All active portfolios with their holdings — the valuation job's working set.</summary>
    Task<IReadOnlyList<GoalPortfolio>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(GoalPortfolio portfolio, CancellationToken cancellationToken = default);
}
