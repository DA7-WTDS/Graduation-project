namespace Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;

public sealed record PortfolioResponse(
    Guid Id,
    Guid UserId,
    string PrimaryGoal,
    string TimeHorizon,
    int RiskTolerance,
    string MarketReaction,
    string InvestmentExperience,
    int StocksPercentage,
    int BondsPercentage,
    int EtfsPercentage,
    int CashPercentage,
    string RiskProfile,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
