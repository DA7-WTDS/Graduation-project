using Project.Modules.Recommendations.Domain.Holdings;

namespace Project.Modules.Recommendations.Application.Abstractions.Holdings;

public interface IUserHoldingRepository
{
    Task<IReadOnlyList<UserHolding>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the user's entire holdings set (delete-all + add). Caller saves via IUnitOfWork.</summary>
    Task ReplaceForUserAsync(Guid userId, IEnumerable<UserHolding> holdings, CancellationToken cancellationToken = default);
}
