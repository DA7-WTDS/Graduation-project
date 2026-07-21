using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Shadow;

/// <summary>
/// One day's NAV point for a shadow portfolio (§ 6.1) — the append-only series
/// the public track record reads. One row per portfolio per valued day.
/// </summary>
public sealed class ShadowSnapshot : Entity
{
    private ShadowSnapshot() { }

    public Guid Id { get; private set; }
    public Guid ShadowPortfolioId { get; private set; }
    public DateOnly Date { get; private set; }
    public double Nav { get; private set; }
    public double DailyReturn { get; private set; }
    public bool Rebalanced { get; private set; }

    public static ShadowSnapshot Create(Guid portfolioId, DateOnly date, double nav, double dailyReturn, bool rebalanced) =>
        new()
        {
            Id = Guid.NewGuid(),
            ShadowPortfolioId = portfolioId,
            Date = date,
            Nav = nav,
            DailyReturn = dailyReturn,
            Rebalanced = rebalanced,
        };
}
