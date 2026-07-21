using Project.Common.Domain.Abstractions;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Domain.Shadow;

/// <summary>
/// A live paper portfolio, one per strategy template, run daily from before we
/// have users so the public track record launches with real history (§ 6.1).
/// Not tied to any goal or user: a fixed-notional "model portfolio, costs
/// simulated" that experiences exactly what a real investor on that template
/// would, rebalances and drawdowns included.
/// </summary>
public sealed class ShadowPortfolio : Entity
{
    private readonly List<ShadowPosition> _positions = [];

    private ShadowPortfolio() { }

    public Guid Id { get; private set; }
    public string TemplateKey { get; private set; } = string.Empty;
    public string TemplateName { get; private set; } = string.Empty;
    public string Market { get; private set; } = "us";
    public RiskProfile RiskBand { get; private set; }
    public string RebalanceCadence { get; private set; } = "monthly";
    public double DrawdownAlertPct { get; private set; }

    public decimal Notional { get; private set; }
    public double CashBalance { get; private set; }
    public double LastNav { get; private set; }
    public double HighWaterMarkNav { get; private set; }

    public DateOnly InceptionDate { get; private set; }
    public DateOnly? LastValuedOn { get; private set; }
    public DateOnly? LastRebalancedOn { get; private set; }
    public bool DrawdownAlertActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<ShadowPosition> Positions => _positions.AsReadOnly();

    public static ShadowPortfolio Create(
        string templateKey,
        string templateName,
        string market,
        RiskProfile riskBand,
        string rebalanceCadence,
        double drawdownAlertPct,
        decimal notional,
        DateOnly inceptionDate)
    {
        return new ShadowPortfolio
        {
            Id = Guid.NewGuid(),
            TemplateKey = templateKey,
            TemplateName = templateName,
            Market = market,
            RiskBand = riskBand,
            RebalanceCadence = rebalanceCadence,
            DrawdownAlertPct = drawdownAlertPct,
            Notional = notional,
            CashBalance = (double)notional, // starts fully in cash, invested on the first rebalance
            LastNav = (double)notional,
            HighWaterMarkNav = (double)notional,
            InceptionDate = inceptionDate,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Has this portfolio ever traded? False only between creation and
    /// its first rebalance (inception buy).</summary>
    public bool IsInvested => _positions.Count > 0;

    /// <summary>
    /// Replace the book after a rebalance: new lots, residual cash, and the NAV
    /// it was valued at. Lifts the high-water mark and stamps both valuation and
    /// rebalance dates.
    /// </summary>
    public void ApplyRebalance(IEnumerable<ShadowLot> lots, double cash, double nav, DateOnly asOf)
    {
        _positions.Clear();
        foreach (ShadowLot lot in lots)
        {
            _positions.Add(ShadowPosition.Create(Id, lot.Symbol, lot.Sleeve, lot.Shares, lot.AvgCost));
        }

        CashBalance = cash;
        MarkValued(nav, asOf);
        LastRebalancedOn = asOf;
    }

    /// <summary>Mark-to-market on a non-rebalance day: NAV moved with prices, no
    /// trades. Lifts the high-water mark and stamps the valuation date.</summary>
    public void ApplyValuation(double nav, DateOnly asOf) => MarkValued(nav, asOf);

    private void MarkValued(double nav, DateOnly asOf)
    {
        LastNav = nav;
        if (nav > HighWaterMarkNav)
        {
            HighWaterMarkNav = nav;
        }
        LastValuedOn = asOf;
    }

    /// <summary>Fire-once-then-rearm drawdown flag (same hysteresis as the live
    /// portfolios, § 4.2). The shadow book only logs these, but it must experience
    /// them so the model history is honest. Returns true on the crossing edge.</summary>
    public bool EvaluateDrawdownAlert(double drawdown)
    {
        if (drawdown >= DrawdownAlertPct)
        {
            if (DrawdownAlertActive)
            {
                return false;
            }
            DrawdownAlertActive = true;
            return true;
        }

        DrawdownAlertActive = false;
        return false;
    }
}
