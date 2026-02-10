using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Portfolios;

public sealed class Portfolio : Entity
{
    private Portfolio() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string PrimaryGoal { get; private set; }
    public string TimeHorizon { get; private set; }
    public int RiskTolerance { get; private set; }
    public string MarketReaction { get; private set; }
    public string InvestmentExperience { get; private set; }
    public int StocksPercentage { get; private set; }
    public int BondsPercentage { get; private set; }
    public int EtfsPercentage { get; private set; }
    public int CashPercentage { get; private set; }
    public RiskProfile RiskProfile { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Portfolio Create(Guid userId)
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        portfolio.Raise(new PortfolioCreatedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, portfolio.Id, userId));

        return portfolio;
    }

    public void Update(string primaryGoal, string timeHorizon, int riskTolerance, string marketReaction, string investmentExperience, int stocksPercentage, int bondsPercentage, int etfsPercentage, int cashPercentage, RiskProfile riskProfile)
    {
        PrimaryGoal = primaryGoal;
        TimeHorizon = timeHorizon;
        RiskTolerance = riskTolerance;
        MarketReaction = marketReaction;
        InvestmentExperience = investmentExperience;
        StocksPercentage = stocksPercentage;
        BondsPercentage = bondsPercentage;
        EtfsPercentage = etfsPercentage;
        CashPercentage = cashPercentage;
        RiskProfile = riskProfile;

        UpdatedAt = DateTime.UtcNow;
    }
}
