using FluentAssertions;
using Project.Modules.Portfolio.Domain.Allocation;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Allocation;

// § 3.3: the optimizer is the product's promise of explainability — every rule
// here (determinism, caps, risk filter, folds) is user-facing behavior.
public class AllocationOptimizerTests
{
    private static Instrument Stock(string symbol, double vol, string sector = "Technology")
    {
        var i = Instrument.Create("us", symbol, InstrumentType.Stock, AssetClass.Equity, "USD", [Sleeves.Core], sector);
        i.UpdateStats(vol, 1e9, 100, sector, DateTime.UtcNow);
        return i;
    }

    private static Instrument Etf(string symbol, AssetClass assetClass, double vol)
    {
        var i = Instrument.Create("us", symbol, InstrumentType.Etf, assetClass, "USD", [Sleeves.Stability]);
        i.UpdateStats(vol, 1e9, 100, null, DateTime.UtcNow);
        return i;
    }

    private static readonly List<Instrument> Registry =
    [
        Etf("SPY", AssetClass.Equity, 0.13),
        Etf("GLD", AssetClass.Gold, 0.14),
        Etf("AGG", AssetClass.FixedIncome, 0.05),
        Etf("BIL", AssetClass.CashLike, 0.002),
        Stock("AAA", 0.20, "Technology"),
        Stock("BBB", 0.25, "Healthcare"),
        Stock("CCC", 0.30, "Energy"),
        Stock("DDD", 0.40, "Technology"),
    ];

    private static List<RankedEquity> Rankings(params (string Symbol, double Score, string Risk)[] rows) =>
        rows.Select(r => new RankedEquity(r.Symbol, r.Score, "UP", r.Risk)).ToList();

    private static readonly List<TemplateBucket> BalancedBuckets =
    [
        new(Sleeves.Core, 0.50, new BucketRules(Types: ["stock"])),
        new(Sleeves.Stability, 0.30, new BucketRules(AssetClasses: ["gold", "fixed_income"])),
        new(Sleeves.Stability, 0.20, new BucketRules(AssetClasses: ["cash_like"])),
    ];

    [Fact]
    public void Same_inputs_produce_the_identical_portfolio_and_hash()
    {
        var rankings = Rankings(("AAA", 80, "LOW"), ("BBB", 70, "MEDIUM"), ("CCC", 60, "HIGH"));

        AllocationResult first = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, Registry, rankings, 10_000m);
        AllocationResult second = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, Registry, rankings, 10_000m);

        second.InputsHash.Should().Be(first.InputsHash);
        second.Positions.Should().BeEquivalentTo(first.Positions, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Weights_sum_to_one_and_respect_the_bucket_split()
    {
        // Four names so the 50% core bucket is feasible under the 15% position cap.
        var rankings = Rankings(("AAA", 80, "LOW"), ("BBB", 70, "LOW"), ("CCC", 60, "LOW"), ("DDD", 50, "LOW"));

        AllocationResult result = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, Registry, rankings, 10_000m);

        result.Positions.Sum(p => p.Weight).Should().BeApproximately(1.0, 1e-9);
        result.Positions.Where(p => p.Sleeve == Sleeves.Core).Sum(p => p.Weight).Should().BeApproximately(0.50, 1e-9);
        // Stability 30% split across gold + fixed income, 20% cash — one instrument per asset class.
        result.Positions.Single(p => p.Symbol == "GLD").Weight.Should().BeApproximately(0.15, 1e-9);
        result.Positions.Single(p => p.Symbol == "AGG").Weight.Should().BeApproximately(0.15, 1e-9);
        result.Positions.Single(p => p.Symbol == "BIL").Weight.Should().BeApproximately(0.20, 1e-9);
    }

    [Fact]
    public void Conservative_profiles_never_receive_medium_or_high_risk_names()
    {
        var rankings = Rankings(("AAA", 80, "LOW"), ("BBB", 90, "MEDIUM"), ("CCC", 95, "HIGH"));

        AllocationResult result = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Conservative, Registry, rankings, 10_000m);

        List<string> coreSymbols = result.Positions.Where(p => p.Sleeve == Sleeves.Core).Select(p => p.Symbol).ToList();
        coreSymbols.Should().BeEquivalentTo(["AAA"]);
    }

    [Fact]
    public void Down_direction_names_are_never_bought()
    {
        var rankings = new List<RankedEquity>
        {
            new("AAA", 80, "UP", "LOW"),
            new("BBB", 95, "DOWN", "LOW"), // best score but model says down
        };

        AllocationResult result = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Aggressive, Registry, rankings, 10_000m);

        result.Positions.Select(p => p.Symbol).Should().NotContain("BBB");
    }

    [Fact]
    public void Inverse_vol_tilt_gives_the_calmer_of_two_equally_ranked_names_more_weight()
    {
        var rankings = Rankings(("AAA", 80, "LOW"), ("DDD", 80, "LOW")); // vol 0.20 vs 0.40

        // Caps loosened so only the inverse-vol tilt drives the ratio.
        AllocationResult result = AllocationOptimizer.Build(
            BalancedBuckets, RiskProfile.Aggressive, Registry, rankings, 10_000m,
            new AllocationOptions(MaxPositionWeight: 0.50, MaxSectorWeight: 1.0));

        double aaa = result.Positions.Single(p => p.Symbol == "AAA").Weight;
        double ddd = result.Positions.Single(p => p.Symbol == "DDD").Weight;
        aaa.Should().BeGreaterThan(ddd);
        (aaa / ddd).Should().BeApproximately(2.0, 0.01); // same score, half the vol → double the weight
    }

    [Fact]
    public void No_single_position_exceeds_the_position_cap()
    {
        // One name with an overwhelming score would take nearly the whole core.
        var rankings = Rankings(("AAA", 1000, "LOW"), ("BBB", 1, "LOW"), ("CCC", 1, "HIGH"));

        AllocationResult result = AllocationOptimizer.Build(
            BalancedBuckets, RiskProfile.Aggressive, Registry, rankings, 10_000m,
            new AllocationOptions(MaxPositionWeight: 0.15));

        foreach (AllocationPosition p in result.Positions.Where(p => p.Sleeve == Sleeves.Core))
        {
            p.Weight.Should().BeLessThanOrEqualTo(0.15 + 1e-6);
        }
    }

    [Fact]
    public void Tactical_and_speculative_buckets_fold_into_core_until_their_engines_ship()
    {
        List<TemplateBucket> activeGrowth =
        [
            new(Sleeves.Core, 0.50, new BucketRules(Types: ["stock"])),
            new(Sleeves.Tactical, 0.30, new BucketRules(Types: ["stock"])),
            new(Sleeves.Speculative, 0.10, new BucketRules(Types: ["stock"])),
            new(Sleeves.Stability, 0.10, new BucketRules(AssetClasses: ["cash_like"])),
        ];
        var rankings = Rankings(("AAA", 80, "LOW"), ("BBB", 70, "LOW"), ("CCC", 60, "LOW"), ("DDD", 50, "LOW"));

        AllocationResult result = AllocationOptimizer.Build(
            activeGrowth, RiskProfile.Aggressive, Registry, rankings, 10_000m,
            new AllocationOptions(MaxPositionWeight: 0.30));

        result.Positions.Where(p => p.Sleeve == Sleeves.Core).Sum(p => p.Weight).Should().BeApproximately(0.90, 1e-9);
        result.Assumptions.Should().Contain(a => a.Contains("tactical"));
        result.Assumptions.Should().Contain(a => a.Contains("speculative"));
    }

    [Fact]
    public void Unfillable_bucket_weight_is_redistributed_and_disclosed()
    {
        // Registry without any gold/fixed-income instrument.
        List<Instrument> thin = [Etf("BIL", AssetClass.CashLike, 0.002), Stock("AAA", 0.20)];
        var rankings = Rankings(("AAA", 80, "LOW"));

        AllocationResult result = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, thin, rankings, 10_000m);

        result.Positions.Sum(p => p.Weight).Should().BeApproximately(1.0, 1e-9);
        result.Assumptions.Should().Contain(a => a.Contains("redistributed"));
    }

    [Fact]
    public void Empty_rankings_still_produce_a_valid_defensive_portfolio()
    {
        AllocationResult result = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, Registry, [], 10_000m);

        result.Positions.Should().NotBeEmpty();
        result.Positions.Sum(p => p.Weight).Should().BeApproximately(1.0, 1e-9);
        result.Positions.Should().OnlyContain(p => p.Sleeve == Sleeves.Stability);
        result.Assumptions.Should().Contain(a => a.Contains("core"));
    }

    [Fact]
    public void Dust_positions_are_dropped_for_small_amounts()
    {
        var rankings = Rankings(("AAA", 100, "LOW"), ("BBB", 1, "LOW")); // BBB would get pennies

        AllocationResult result = AllocationOptimizer.Build(
            BalancedBuckets, RiskProfile.Aggressive, Registry, rankings, 200m,
            new AllocationOptions(MaxPositionWeight: 0.50, MaxSectorWeight: 1.0, MinPositionValue: 25m));

        result.Positions.Where(p => p.Sleeve == Sleeves.Core).Select(p => p.Symbol).Should().NotContain("BBB");
    }

    [Fact]
    public void Broad_etf_core_buckets_are_exempt_from_the_single_stock_position_cap()
    {
        // The retirement template: 40% broad equity ETF. The 15% cap targets
        // single-stock concentration and must not chop the index bucket.
        List<TemplateBucket> retirement =
        [
            new(Sleeves.Core, 0.40, new BucketRules(AssetClasses: ["equity"], Types: ["etf"])),
            new(Sleeves.Stability, 0.25, new BucketRules(AssetClasses: ["gold"])),
            new(Sleeves.Stability, 0.20, new BucketRules(AssetClasses: ["fixed_income"])),
            new(Sleeves.Stability, 0.15, new BucketRules(AssetClasses: ["cash_like"])),
        ];

        AllocationResult result = AllocationOptimizer.Build(retirement, RiskProfile.Moderate, Registry, [], 10_000m);

        result.Positions.Single(p => p.Symbol == "SPY").Weight.Should().BeApproximately(0.40, 1e-9);
        result.Positions.Single(p => p.Symbol == "GLD").Weight.Should().BeApproximately(0.25, 1e-9);
        result.Positions.Single(p => p.Symbol == "AGG").Weight.Should().BeApproximately(0.20, 1e-9);
        result.Positions.Single(p => p.Symbol == "BIL").Weight.Should().BeApproximately(0.15, 1e-9);
        result.Assumptions.Should().BeEmpty();
    }

    [Fact]
    public void Changing_any_input_changes_the_hash()
    {
        var rankings = Rankings(("AAA", 80, "LOW"));

        string baseline = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, Registry, rankings, 10_000m).InputsHash;
        string differentAmount = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Moderate, Registry, rankings, 20_000m).InputsHash;
        string differentBand = AllocationOptimizer.Build(BalancedBuckets, RiskProfile.Aggressive, Registry, rankings, 10_000m).InputsHash;

        baseline.Should().NotBe(differentAmount);
        baseline.Should().NotBe(differentBand);
    }
}
