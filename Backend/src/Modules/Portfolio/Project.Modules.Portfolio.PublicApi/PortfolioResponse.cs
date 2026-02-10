namespace Project.Modules.Portfolio.PublicApi;

public sealed record PortfolioResponse(
    Guid Id,
    Guid UserId,
    string RiskProfile,
    int StocksPercentage,
    int BondsPercentage,
    int EtfsPercentage,
    int CashPercentage
);
