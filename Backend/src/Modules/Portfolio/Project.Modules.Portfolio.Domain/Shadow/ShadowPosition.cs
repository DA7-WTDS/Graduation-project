using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Shadow;

/// <summary>One holding in a <see cref="ShadowPortfolio"/> (§ 6.1).</summary>
public sealed class ShadowPosition : Entity
{
    private ShadowPosition() { }

    public Guid Id { get; private set; }
    public Guid ShadowPortfolioId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string Sleeve { get; private set; } = string.Empty;
    public double Shares { get; private set; }
    public double AvgCost { get; private set; }

    public static ShadowPosition Create(Guid portfolioId, string symbol, string sleeve, double shares, double avgCost) =>
        new()
        {
            Id = Guid.NewGuid(),
            ShadowPortfolioId = portfolioId,
            Symbol = symbol,
            Sleeve = sleeve,
            Shares = shares,
            AvgCost = avgCost,
        };
}
