namespace Project.Modules.Portfolio.PublicApi;

public interface IPortfolioApi
{
    Task<PortfolioResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Every user who has completed onboarding (has a portfolio). Used to fan out daily notifications.</summary>
    Task<IReadOnlyList<PortfolioResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
