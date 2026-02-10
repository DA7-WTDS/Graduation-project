using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Portfolios.CreatePortfolio;

public sealed record CreatePortfolioCommand(
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
    string RiskProfile) : ICommand<Guid>;
