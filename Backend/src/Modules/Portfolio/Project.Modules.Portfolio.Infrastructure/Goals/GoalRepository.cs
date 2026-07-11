using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Goals;

internal sealed class GoalRepository(PortfolioDbContext dbContext) : IGoalRepository
{
    public async Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Goals.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Goal>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Goals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<InvestorProfile?> GetLatestProfileAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.InvestorProfiles
            .Where(p => p.GoalId == goalId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddGoalAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        await dbContext.Goals.AddAsync(goal, cancellationToken);
    }

    public async Task AddResponseAsync(QuestionnaireResponse response, CancellationToken cancellationToken = default)
    {
        await dbContext.QuestionnaireResponses.AddAsync(response, cancellationToken);
    }

    public async Task AddProfileAsync(InvestorProfile profile, CancellationToken cancellationToken = default)
    {
        await dbContext.InvestorProfiles.AddAsync(profile, cancellationToken);
    }
}
