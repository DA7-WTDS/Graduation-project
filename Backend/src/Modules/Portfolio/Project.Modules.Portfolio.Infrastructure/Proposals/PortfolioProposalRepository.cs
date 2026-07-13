using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Proposals;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Proposals;

internal sealed class PortfolioProposalRepository(PortfolioDbContext dbContext) : IPortfolioProposalRepository
{
    public async Task<PortfolioProposal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.PortfolioProposals.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PortfolioProposal>> GetByGoalIdAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PortfolioProposals
            .Where(p => p.GoalId == goalId)
            .OrderByDescending(p => p.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLatestVersionAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PortfolioProposals
            .Where(p => p.GoalId == goalId)
            .Select(p => (int?)p.Version)
            .MaxAsync(cancellationToken) ?? 0;
    }

    public async Task<IReadOnlyList<PortfolioProposal>> GetAcceptedByGoalIdAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PortfolioProposals
            .Where(p => p.GoalId == goalId && p.Status == ProposalStatus.Accepted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PortfolioProposal proposal, CancellationToken cancellationToken = default)
    {
        await dbContext.PortfolioProposals.AddAsync(proposal, cancellationToken);
    }
}
