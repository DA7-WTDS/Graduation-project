using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Portfolios;

public enum GoalPortfolioStatus
{
    /// <summary>The user's live target, valued nightly.</summary>
    Active,

    /// <summary>Replaced by a newer accepted proposal.</summary>
    Closed
}

/// <summary>One position inside an active portfolio: the target weight the
/// optimizer set, plus the entry price captured at acceptance so the position
/// can be valued and its drift measured as prices move.</summary>
public sealed class PortfolioHolding : Entity
{
    private PortfolioHolding() { }

    public Guid Id { get; private set; }
    public Guid GoalPortfolioId { get; private set; }
    public string Symbol { get; private set; }
    public string Sleeve { get; private set; }
    public double TargetWeight { get; private set; }
    public double EntryPrice { get; private set; }
    public double Shares { get; private set; }

    internal static PortfolioHolding Create(string symbol, string sleeve, double targetWeight, double entryPrice, double shares)
    {
        return new PortfolioHolding
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Sleeve = sleeve,
            TargetWeight = targetWeight,
            EntryPrice = entryPrice,
            Shares = shares
        };
    }
}

/// <summary>
/// A user's live portfolio for a goal (Phase 4.2): the accepted proposal turned
/// into valued positions. The nightly valuation job marks it to market, tracks
/// the high-water mark, and drives the drawdown and drift triggers (§ 3.5).
/// Accepting a newer proposal closes this one and opens a fresh one — a restart
/// with a new inception and high-water mark, which is what a rebalance is.
/// </summary>
public sealed class GoalPortfolio : Entity
{
    private readonly List<PortfolioHolding> _holdings = [];

    private GoalPortfolio() { }

    public Guid Id { get; private set; }
    public Guid GoalId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProposalId { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>Drawdown that fires an alert (from the template, e.g. 0.15).</summary>
    public double DrawdownThreshold { get; private set; }

    public double HighWaterMarkNav { get; private set; }
    public double LastNav { get; private set; }
    public DateTime? LastValuedAt { get; private set; }

    // Hysteresis flags: an alert fires once on crossing and re-arms only after
    // the condition clears, so a persisting drawdown/drift doesn't spam nightly.
    public bool DrawdownAlertActive { get; private set; }
    public bool DriftAlertActive { get; private set; }

    public GoalPortfolioStatus Status { get; private set; }
    public DateTime InceptionDate { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    /// <summary>When the last periodic digest went out; null until the first one.</summary>
    public DateTime? LastDigestAt { get; private set; }

    public IReadOnlyList<PortfolioHolding> Holdings => _holdings;

    public static GoalPortfolio Open(
        Guid goalId,
        Guid userId,
        Guid proposalId,
        decimal amount,
        double drawdownThreshold,
        IEnumerable<(string Symbol, string Sleeve, double TargetWeight, double EntryPrice)> positions)
    {
        var portfolio = new GoalPortfolio
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            UserId = userId,
            ProposalId = proposalId,
            Amount = amount,
            DrawdownThreshold = drawdownThreshold,
            Status = GoalPortfolioStatus.Active,
            InceptionDate = DateTime.UtcNow
        };

        foreach ((string symbol, string sleeve, double targetWeight, double entryPrice) in positions)
        {
            // shares from the money put into the sleeve at the entry price.
            double positionValue = (double)amount * targetWeight;
            double shares = entryPrice > 0 ? positionValue / entryPrice : 0;
            portfolio._holdings.Add(PortfolioHolding.Create(symbol, sleeve, targetWeight, entryPrice, shares));
        }

        double nav = portfolio._holdings.Sum(h => h.Shares * h.EntryPrice);
        portfolio.HighWaterMarkNav = nav;
        portfolio.LastNav = nav;
        return portfolio;
    }

    public void Close()
    {
        Status = GoalPortfolioStatus.Closed;
        ClosedAt = DateTime.UtcNow;
    }

    /// <summary>Applies a nightly valuation, updating NAV and the high-water mark.</summary>
    public void ApplyValuation(double nav, DateTime asOf)
    {
        LastNav = nav;
        LastValuedAt = asOf;
        if (nav > HighWaterMarkNav)
        {
            HighWaterMarkNav = nav;
        }
    }

    /// <summary>Drawdown crossing with hysteresis: fires once when it breaches
    /// the threshold, re-arms only after recovering above it.</summary>
    public bool EvaluateDrawdownAlert(double drawdown)
    {
        if (drawdown >= DrawdownThreshold && !DrawdownAlertActive)
        {
            DrawdownAlertActive = true;
            return true;
        }

        if (drawdown < DrawdownThreshold)
        {
            DrawdownAlertActive = false;
        }

        return false;
    }

    public void MarkDigestSent(DateTime at) => LastDigestAt = at;

    public bool EvaluateDriftAlert(double maxDrift, double driftThreshold)
    {
        if (maxDrift >= driftThreshold && !DriftAlertActive)
        {
            DriftAlertActive = true;
            return true;
        }

        if (maxDrift < driftThreshold)
        {
            DriftAlertActive = false;
        }

        return false;
    }
}
