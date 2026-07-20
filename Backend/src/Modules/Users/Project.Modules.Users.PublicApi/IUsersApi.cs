namespace Project.Modules.Users.PublicApi;

public interface IUsersApi
{
    Task<UserResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ids of Admin-role users — the ops-alert audience (§ 6.2).</summary>
    Task<IReadOnlyList<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);
}

