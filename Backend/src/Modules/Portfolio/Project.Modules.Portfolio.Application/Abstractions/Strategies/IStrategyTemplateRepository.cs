using Project.Modules.Portfolio.Domain.Strategies;

namespace Project.Modules.Portfolio.Application.Abstractions.Strategies;

public interface IStrategyTemplateRepository
{
    Task<IReadOnlyList<StrategyTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);
}
