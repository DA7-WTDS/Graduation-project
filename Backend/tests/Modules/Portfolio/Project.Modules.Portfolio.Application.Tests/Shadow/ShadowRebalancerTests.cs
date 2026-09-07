using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Project.Modules.Portfolio.Domain.Shadow;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Shadow;

public class ShadowRebalancerTests
{
    private static readonly Dictionary<string, double> Prices = new()
    {
        ["SPY"] = 100, ["GLD"] = 50, ["AGG"] = 25,
    };

    [Fact]
    public void Inception_buy_from_cash_costs_notional_times_one_side()
    {
        // Whole book bought from cash: every dollar deployed is one dollar traded,
        // charged 25 bps once — matches backtest.py buying in from a flat start.
        var targets = new List<ShadowTarget>
        {
            new("SPY", "core", 0.60),
            new("GLD", "stability", 0.40),
        };

        RebalanceResult r = ShadowRebalancer.Rebalance([], targets, Prices, cash: 100_000);

        r.NavBefore.Should().Be(100_000);
        r.TradedValue.Should().BeApproximately(100_000, 1e-6);          // 100% turnover
        r.Cost.Should().BeApproximately(100_000 * 0.0025, 1e-6);        // 250
        r.NavAfter.Should().BeApproximately(99_750, 1e-6);
    }

    [Fact]
    public void Book_is_consistent_after_rebalance()
    {
        var targets = new List<ShadowTarget> { new("SPY", "core", 0.60), new("GLD", "stability", 0.40) };
        RebalanceResult r = ShadowRebalancer.Rebalance([], targets, Prices, cash: 100_000);

        double invested = r.Lots.Sum(l => l.Shares * Prices[l.Symbol]);
        (invested + r.Cash).Should().BeApproximately(r.NavAfter, 1e-6);

        // Weights land on target (of post-cost NAV).
        r.Lots.Single(l => l.Symbol == "SPY").Shares.Should().BeApproximately(0.60 * r.NavAfter / 100, 1e-6);
        r.Lots.Single(l => l.Symbol == "SPY").AvgCost.Should().Be(100); // basis = today's price
    }

    [Fact]
    public void Only_the_traded_difference_incurs_cost_on_a_later_rebalance()
    {
        // Already 60/40 SPY/GLD at 100k; rebalance to 50/50 trades only |Δ|.
        var current = new List<ShadowLot>
        {
            new("SPY", "core", 600, 100),   // 60,000
            new("GLD", "stability", 800, 50), // 40,000
        };
        var targets = new List<ShadowTarget> { new("SPY", "core", 0.50), new("GLD", "stability", 0.50) };

        RebalanceResult r = ShadowRebalancer.Rebalance(current, targets, Prices, cash: 0);

        r.NavBefore.Should().BeApproximately(100_000, 1e-6);
        // SPY 60k→50k (10k), GLD 40k→50k (10k): 20k traded.
        r.TradedValue.Should().BeApproximately(20_000, 1e-6);
        r.Cost.Should().BeApproximately(20_000 * 0.0025, 1e-6);         // 50
    }

    [Fact]
    public void Exiting_a_name_counts_its_full_value_as_traded()
    {
        var current = new List<ShadowLot> { new("AGG", "stability", 4000, 25) }; // 100,000
        var targets = new List<ShadowTarget> { new("SPY", "core", 1.0) };

        RebalanceResult r = ShadowRebalancer.Rebalance(current, targets, Prices, cash: 0);

        // Sell 100k of AGG + buy 100k of SPY = 200k traded (both sides).
        r.TradedValue.Should().BeApproximately(200_000, 1e-6);
        r.Cost.Should().BeApproximately(200_000 * 0.0025, 1e-6);
        r.Lots.Should().ContainSingle(l => l.Symbol == "SPY");
        r.Lots.Should().NotContain(l => l.Symbol == "AGG");
    }

    [Fact]
    public void Nav_marks_to_market_without_trading()
    {
        var lots = new List<ShadowLot> { new("SPY", "core", 600, 100), new("GLD", "stability", 800, 50) };
        var pricesUp = new Dictionary<string, double> { ["SPY"] = 110, ["GLD"] = 50 };

        double nav = ShadowRebalancer.Nav(lots, pricesUp, cash: 0);

        nav.Should().BeApproximately(600 * 110 + 800 * 50, 1e-6); // 106,000
    }

    [Fact]
    public void Zero_weight_targets_are_dropped()
    {
        var targets = new List<ShadowTarget> { new("SPY", "core", 1.0), new("GLD", "stability", 0.0) };
        RebalanceResult r = ShadowRebalancer.Rebalance([], targets, Prices, cash: 100_000);

        r.Lots.Should().ContainSingle().Which.Symbol.Should().Be("SPY");
    }

    [Fact]
    public void The_same_instrument_from_two_sleeves_becomes_one_merged_holding()
    {
        // The optimizer can legitimately reach one instrument from two buckets: a broad
        // equity-ETF core and a named fixed-income stability sleeve both selecting AGG.
        // That is one holding whose weight is the sum. Before merging, this threw
        // "same key has already been added" and took the entire nightly run down.
        var targets = new List<ShadowTarget>
        {
            new("AGG", "core", 0.30),
            new("AGG", "stability", 0.20),
            new("SPY", "core", 0.50),
        };
        var prices = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["AGG"] = 100, ["SPY"] = 400,
        };

        RebalanceResult result = ShadowRebalancer.Rebalance([], targets, prices, cash: 10_000);

        result.Lots.Should().HaveCount(2);
        ShadowLot agg = result.Lots.Single(l => l.Symbol == "AGG");
        ShadowLot spy = result.Lots.Single(l => l.Symbol == "SPY");
        // 50% of the book, i.e. the two sleeves summed, not one of them and not double.
        (agg.Shares * 100).Should().BeApproximately(spy.Shares * 400, 1e-6);
    }

    [Fact]
    public void A_merged_holding_is_labelled_with_its_largest_sleeve()
    {
        var targets = new List<ShadowTarget>
        {
            new("AGG", "stability", 0.10),
            new("AGG", "core", 0.40),
        };
        var prices = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["AGG"] = 100 };

        RebalanceResult result = ShadowRebalancer.Rebalance([], targets, prices, cash: 10_000);

        result.Lots.Single().Sleeve.Should().Be("core");
    }

    [Fact]
    public void Merging_does_not_double_count_traded_value()
    {
        // Cost is charged on notional traded. If the merge happened after the turnover
        // sum, a split holding would be charged twice for one position.
        var split = new List<ShadowTarget> { new("AGG", "core", 0.5), new("AGG", "stability", 0.5) };
        var whole = new List<ShadowTarget> { new("AGG", "core", 1.0) };
        var prices = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["AGG"] = 100 };

        RebalanceResult a = ShadowRebalancer.Rebalance([], split, prices, cash: 10_000);
        RebalanceResult b = ShadowRebalancer.Rebalance([], whole, prices, cash: 10_000);

        a.Cost.Should().BeApproximately(b.Cost, 1e-9);
        a.NavAfter.Should().BeApproximately(b.NavAfter, 1e-9);
    }

}
