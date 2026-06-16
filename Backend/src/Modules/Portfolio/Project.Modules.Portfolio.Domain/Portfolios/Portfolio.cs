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

    /// <summary>The capital the user intends to invest, used to turn allocation
    /// percentages into per-pick dollar amounts. Defaults to 0 until the user sets it.</summary>
    public decimal InvestmentAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Portfolio Create(
        Guid userId,
        string primaryGoal,
        string timeHorizon,
        int riskTolerance,
        string marketReaction,
        string investmentExperience,
        int stocksPercentage,
        int bondsPercentage,
        int etfsPercentage,
        int cashPercentage,
        RiskProfile riskProfile,
        decimal investmentAmount = 0m)
    {
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PrimaryGoal = primaryGoal,
            TimeHorizon = timeHorizon,
            RiskTolerance = riskTolerance,
            MarketReaction = marketReaction,
            InvestmentExperience = investmentExperience,
            StocksPercentage = stocksPercentage,
            BondsPercentage = bondsPercentage,
            EtfsPercentage = etfsPercentage,
            CashPercentage = cashPercentage,
            RiskProfile = riskProfile,
            InvestmentAmount = investmentAmount,
            CreatedAt = DateTime.UtcNow
        };

        portfolio.Raise(new PortfolioCreatedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, portfolio.Id, userId));

        return portfolio;
    }

    public void Update(string primaryGoal, string timeHorizon, int riskTolerance, string marketReaction, string investmentExperience, int stocksPercentage, int bondsPercentage, int etfsPercentage, int cashPercentage, RiskProfile riskProfile, decimal investmentAmount = 0m)
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
        InvestmentAmount = investmentAmount;

        UpdatedAt = DateTime.UtcNow;
    }
}
