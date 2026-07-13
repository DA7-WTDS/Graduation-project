using Project.Modules.Portfolio.Domain.Proposals;

namespace Project.Modules.Portfolio.Application.Abstractions.Proposals;

public interface IPortfolioProposalRepository
{
    Task<PortfolioProposal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortfolioProposal>> GetByGoalIdAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>Highest existing version for the goal, or 0 if none.</summary>
    Task<int> GetLatestVersionAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>Currently accepted proposals for the goal (normally 0 or 1).</summary>
    Task<IReadOnlyList<PortfolioProposal>> GetAcceptedByGoalIdAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task AddAsync(PortfolioProposal proposal, CancellationToken cancellationToken = default);
}
