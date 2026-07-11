using FluentAssertions;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Strategies;
using System.Collections.Generic;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Strategies;

// § 3.2 selection: mirrors the seeded template set — if these rules change,
// change the seeds and these tests together.
public class TemplateSelectorTests
{
    private static readonly List<TemplateBucket> AnyBuckets =
        [new(Sleeves.Core, 1.0, new BucketRules(Types: ["stock"]))];

    private static readonly List<StrategyTemplate> Seeded =
    [
        StrategyTemplate.Create("retirement_set_and_forget", "Retirement", ["Retirement"], 0, 100, false, AnyBuckets, "semi_annual", 0.15),
        StrategyTemplate.Create("balanced_growth", "Balanced", ["LongTermWealth", "MediumTermGoal", "SpeculationLearning"], 0, 69, false, AnyBuckets, "monthly", 0.12),
        StrategyTemplate.Create("active_growth", "Active", ["LongTermWealth", "MediumTermGoal", "SpeculationLearning"], 70, 100, false, AnyBuckets, "weekly", 0.20),
        StrategyTemplate.Create("speculative_gated", "Speculative", ["SpeculationLearning"], 70, 100, true, AnyBuckets, "weekly", 0.25),
    ];

    [Theory]
    [InlineData("Retirement", 100, false, "retirement_set_and_forget")] // goal dominates risk
    [InlineData("Retirement", 10, false, "retirement_set_and_forget")]
    [InlineData("LongTermWealth", 35, false, "balanced_growth")]        // conservative → balanced (defensive weights)
    [InlineData("LongTermWealth", 69, false, "balanced_growth")]
    [InlineData("LongTermWealth", 70, false, "active_growth")]
    [InlineData("MediumTermGoal", 85, false, "active_growth")]
    [InlineData("SpeculationLearning", 85, true, "speculative_gated")]  // gate satisfied → most specific wins
    [InlineData("SpeculationLearning", 85, false, "active_growth")]     // no opt-in → never speculative
    [InlineData("SpeculationLearning", 50, true, "balanced_growth")]    // unlocked but effective risk too low
    public void Selection_matches_the_seeded_matrix(string goal, int risk, bool unlocked, string expectedKey)
    {
        StrategyTemplate? selected = TemplateSelector.Select(Seeded, goal, risk, unlocked);

        selected.Should().NotBeNull();
        selected!.Key.Should().Be(expectedKey);
    }

    [Fact]
    public void Buckets_round_trip_through_json()
    {
        List<TemplateBucket> buckets =
        [
            new(Sleeves.Core, 0.5, new BucketRules(Types: ["stock"])),
            new(Sleeves.Stability, 0.5, new BucketRules(AssetClasses: ["gold", "cash_like"])),
        ];
        var template = StrategyTemplate.Create("t", "T", ["Retirement"], 0, 100, false, buckets, "monthly", 0.1);

        template.GetBuckets().Should().BeEquivalentTo(buckets);
    }

    [Fact]
    public void Every_goal_risk_combination_has_a_template()
    {
        foreach (string goal in new[] { "Retirement", "LongTermWealth", "MediumTermGoal", "SpeculationLearning" })
        {
            for (int risk = 0; risk <= 100; risk += 5)
            {
                TemplateSelector.Select(Seeded, goal, risk, speculativeUnlocked: false)
                    .Should().NotBeNull($"goal {goal} at risk {risk} must always resolve to a template");
            }
        }
    }
}
