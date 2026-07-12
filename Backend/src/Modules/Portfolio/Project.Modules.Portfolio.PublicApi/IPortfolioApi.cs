namespace Project.Modules.Portfolio.PublicApi;

public interface IPortfolioApi
{
    Task<PortfolioResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Every user who has completed onboarding (has a portfolio). Used to fan out daily notifications.</summary>
    Task<IReadOnlyList<PortfolioResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Monitoring context for one user (§ 3.5): risk band + goal type +
    /// engagement from the latest investor profile. Null when the user has no portfolio.</summary>
    Task<MonitoringProfileResponse?> GetMonitoringProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
