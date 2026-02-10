namespace Project.Modules.Portfolio.PublicApi;

public interface IPortfolioApi
{
    Task<PortfolioResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
