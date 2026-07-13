using FluentAssertions;
using Project.Modules.Portfolio.Domain.Portfolios;
using System.Collections.Generic;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios;

// § 3.5 / 4.2 valuation math + the aggregate's drawdown/drift hysteresis.
public class PortfolioValuationTests
{
    [Fact]
    public void Nav_is_shares_times_price_summed()
    {
        double nav = PortfolioValuation.Nav([(10, 100.0), (5, 20.0)]);
        nav.Should().Be(1100);
    }

    [Theory]
    [InlineData(100, 100, 0.0)]   // at the high
    [InlineData(85, 100, 0.15)]   // 15% down
    [InlineData(120, 100, 0.0)]   // above the high → not a drawdown
    public void Drawdown_is_the_fall_from_the_high_water_mark(double nav, double hwm, double expected)
    {
        PortfolioValuation.Drawdown(nav, hwm).Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void Max_drift_is_the_largest_gap_from_any_target_weight()
    {
        // Target 50/50 but SPY ran to 70% of a 1000 NAV → 20pp drift.
        double drift = PortfolioValuation.MaxDrift(
        [
            ("SPY", 700, 0.5),
            ("AGG", 300, 0.5),
        ]);

        drift.Should().BeApproximately(0.20, 1e-9);
    }

    private static GoalPortfolio Portfolio(double drawdownThreshold = 0.15) => GoalPortfolio.Open(
        System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), 1000m, drawdownThreshold,
        [("SPY", "core", 0.5, 100.0), ("AGG", "stability", 0.5, 100.0)]);

    [Fact]
    public void Opening_sets_shares_from_the_entry_prices_and_nav_equals_amount()
    {
        GoalPortfolio p = Portfolio();

        // 500 into each at 100 → 5 shares each; NAV 1000 = amount.
        p.Holdings.Should().OnlyContain(h => h.Shares == 5);
        p.HighWaterMarkNav.Should().Be(1000);
        p.LastNav.Should().Be(1000);
    }

    [Fact]
    public void Drawdown_alert_fires_once_on_crossing_and_rearms_after_recovery()
    {
        GoalPortfolio p = Portfolio(drawdownThreshold: 0.15);

        p.EvaluateDrawdownAlert(0.10).Should().BeFalse(); // shallow
        p.EvaluateDrawdownAlert(0.18).Should().BeTrue();  // crosses → fire
        p.EvaluateDrawdownAlert(0.20).Should().BeFalse(); // still down → no re-fire
        p.EvaluateDrawdownAlert(0.05).Should().BeFalse(); // recovered → re-arm
        p.EvaluateDrawdownAlert(0.16).Should().BeTrue();  // new crossing → fire again
    }

    [Fact]
    public void Applying_a_new_high_lifts_the_water_mark_so_drawdown_reads_zero()
    {
        GoalPortfolio p = Portfolio();

        p.ApplyValuation(1200, System.DateTime.UtcNow);
        p.HighWaterMarkNav.Should().Be(1200);
        PortfolioValuation.Drawdown(p.LastNav, p.HighWaterMarkNav).Should().Be(0);

        p.ApplyValuation(1020, System.DateTime.UtcNow); // fell from 1200
        p.HighWaterMarkNav.Should().Be(1200);           // HWM sticks
        PortfolioValuation.Drawdown(p.LastNav, p.HighWaterMarkNav).Should().BeApproximately(0.15, 1e-9);
    }

    [Fact]
    public void Drift_alert_has_the_same_fire_once_then_rearm_semantics()
    {
        GoalPortfolio p = Portfolio();

        p.EvaluateDriftAlert(0.05, 0.10).Should().BeFalse();
        p.EvaluateDriftAlert(0.12, 0.10).Should().BeTrue();
        p.EvaluateDriftAlert(0.15, 0.10).Should().BeFalse();
        p.EvaluateDriftAlert(0.04, 0.10).Should().BeFalse(); // rebalanced back
        p.EvaluateDriftAlert(0.11, 0.10).Should().BeTrue();
    }
}
