using System;
using System.Collections.Generic;
using FluentAssertions;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Shadow;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Shadow;

public class ShadowPortfolioTests
{
    private static ShadowPortfolio New(double drawdownAlertPct = 0.15) =>
        ShadowPortfolio.Create("balanced_growth", "Balanced Growth", "us",
            RiskProfile.Moderate, "monthly", drawdownAlertPct, 100_000m,
            new DateOnly(2026, 1, 1));

    [Fact]
    public void Create_starts_fully_in_cash_and_uninvested()
    {
        ShadowPortfolio p = New();

        p.CashBalance.Should().Be(100_000);
        p.LastNav.Should().Be(100_000);
        p.HighWaterMarkNav.Should().Be(100_000);
        p.IsInvested.Should().BeFalse();
        p.Positions.Should().BeEmpty();
    }

    [Fact]
    public void ApplyRebalance_replaces_the_book_and_stamps_dates()
    {
        ShadowPortfolio p = New();
        var lots = new List<ShadowLot> { new("SPY", "core", 500, 100), new("GLD", "stability", 200, 50) };

        p.ApplyRebalance(lots, cash: 100, nav: 99_750, asOf: new DateOnly(2026, 1, 2));

        p.IsInvested.Should().BeTrue();
        p.Positions.Should().HaveCount(2);
        p.CashBalance.Should().Be(100);
        p.LastNav.Should().Be(99_750);
        p.LastRebalancedOn.Should().Be(new DateOnly(2026, 1, 2));
        p.LastValuedOn.Should().Be(new DateOnly(2026, 1, 2));
    }

    [Fact]
    public void High_water_mark_only_ratchets_up()
    {
        ShadowPortfolio p = New();

        p.ApplyValuation(110_000, new DateOnly(2026, 1, 3));
        p.HighWaterMarkNav.Should().Be(110_000);

        p.ApplyValuation(105_000, new DateOnly(2026, 1, 4));
        p.HighWaterMarkNav.Should().Be(110_000); // held, not lowered
        p.LastNav.Should().Be(105_000);
    }

    [Fact]
    public void Drawdown_alert_fires_once_then_rearms()
    {
        ShadowPortfolio p = New(drawdownAlertPct: 0.15);
        p.ApplyValuation(100_000, new DateOnly(2026, 1, 2)); // HWM 100k

        // Down 16% — crosses.
        p.ApplyValuation(84_000, new DateOnly(2026, 1, 3));
        p.EvaluateDrawdownAlert(0.16).Should().BeTrue();
        // Still under water next night — no repeat alert.
        p.EvaluateDrawdownAlert(0.17).Should().BeFalse();

        // Recovers above threshold, then drops again — re-arms and fires.
        p.EvaluateDrawdownAlert(0.05).Should().BeFalse();
        p.EvaluateDrawdownAlert(0.20).Should().BeTrue();
    }
}
