using Project.Modules.Portfolio.Domain.Goals;

namespace Project.Modules.Portfolio.Application.Abstractions.Goals;

public interface IGoalRepository
{
    Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Goal>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<InvestorProfile?> GetLatestProfileAsync(Guid goalId, CancellationToken cancellationToken = default);
    Task AddGoalAsync(Goal goal, CancellationToken cancellationToken = default);
    Task AddResponseAsync(QuestionnaireResponse response, CancellationToken cancellationToken = default);
    Task AddProfileAsync(InvestorProfile profile, CancellationToken cancellationToken = default);
}
