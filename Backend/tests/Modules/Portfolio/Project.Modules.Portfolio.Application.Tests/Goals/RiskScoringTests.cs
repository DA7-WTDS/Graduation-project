using FluentAssertions;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Portfolios;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Goals;

// The scoring engine is the FRA suitability logic — every rule here is a
// promise to the regulator, so every rule gets a test.
public class RiskScoringTests
{
    // A financially bulletproof, fully risk-tolerant investor. Individual tests
    // degrade one dimension at a time from this baseline.
    private static QuestionnaireAnswers Strong(
        int horizonYears = 10,
        bool hasEmergencyFund = true,
        IncomeStability income = IncomeStability.Stable,
        SavingsShareBand savings = SavingsShareBand.LessThanTenPercent,
        MarketReactionAnswer reaction = MarketReactionAnswer.BuyMore,
        ExperienceLevel experience = ExperienceLevel.Experienced,
        bool affordLossConfirmed = false) =>
        new(GoalType.LongTermWealth, horizonYears, 10000m, 500m, hasEmergencyFund,
            income, savings, reaction, experience,
            EngagementLevel.Monthly, UsdComfort.Neutral, affordLossConfirmed);

    [Fact]
    public void Strong_answers_max_out_both_scores()
    {
        RiskScore score = RiskScoring.Score(Strong());

        score.Capacity.Should().Be(100);
        score.Tolerance.Should().Be(100);
        score.EffectiveRisk.Should().Be(100);
        score.RiskBand.Should().Be(RiskProfile.Aggressive);
    }

    [Fact]
    public void Effective_risk_is_the_minimum_of_capacity_and_tolerance()
    {
        // Full capacity, but the person panics and sells everything.
        RiskScore score = RiskScoring.Score(Strong(
            reaction: MarketReactionAnswer.SellAll, experience: ExperienceLevel.None));

        score.Capacity.Should().Be(100);
        score.Tolerance.Should().Be(5);
        score.EffectiveRisk.Should().Be(5);
        score.RiskBand.Should().Be(RiskProfile.Conservative);
    }

    [Fact]
    public void No_emergency_fund_caps_capacity_regardless_of_everything_else()
    {
        RiskScore score = RiskScoring.Score(Strong(hasEmergencyFund: false));

        score.Capacity.Should().Be(35);
        score.RiskBand.Should().Be(RiskProfile.Conservative);
    }

    [Fact]
    public void Betting_most_of_your_savings_caps_capacity()
    {
        RiskScore score = RiskScoring.Score(Strong(savings: SavingsShareBand.MoreThanFiftyPercent));

        score.Capacity.Should().Be(35);
        score.RiskBand.Should().Be(RiskProfile.Conservative);
    }

    [Fact]
    public void No_income_caps_capacity_at_moderate()
    {
        RiskScore score = RiskScoring.Score(Strong(income: IncomeStability.None));

        score.Capacity.Should().Be(50);
        score.EffectiveRisk.Should().Be(50);
        score.RiskBand.Should().Be(RiskProfile.Moderate);
    }

    [Fact]
    public void Mid_range_answers_land_in_the_moderate_band()
    {
        RiskScore score = RiskScoring.Score(Strong(
            horizonYears: 3,
            income: IncomeStability.Variable,
            savings: SavingsShareBand.TenToTwentyFivePercent,
            reaction: MarketReactionAnswer.HoldSteady,
            experience: ExperienceLevel.Beginner));

        // Capacity 20+20+10+15 = 65; tolerance 35+15 = 50 → effective 50.
        score.EffectiveRisk.Should().Be(50);
        score.RiskBand.Should().Be(RiskProfile.Moderate);
    }

    [Fact]
    public void Speculative_sleeve_requires_explicit_opt_in()
    {
        RiskScore withoutOptIn = RiskScoring.Score(Strong(affordLossConfirmed: false));
        RiskScore withOptIn = RiskScoring.Score(Strong(affordLossConfirmed: true));

        withoutOptIn.SpeculativeUnlocked.Should().BeFalse();
        withOptIn.SpeculativeUnlocked.Should().BeTrue();
    }

    [Fact]
    public void Speculative_sleeve_stays_locked_for_beginners_even_with_opt_in()
    {
        RiskScore score = RiskScoring.Score(Strong(
            experience: ExperienceLevel.Beginner, affordLossConfirmed: true));

        score.SpeculativeUnlocked.Should().BeFalse();
    }

    [Fact]
    public void Speculative_sleeve_stays_locked_when_capacity_is_low_even_for_experts()
    {
        // Experienced trader, opted in — but no emergency fund caps capacity at 35.
        RiskScore score = RiskScoring.Score(Strong(
            hasEmergencyFund: false, affordLossConfirmed: true));

        score.SpeculativeUnlocked.Should().BeFalse();
    }

    [Fact]
    public void Same_answers_always_produce_the_same_score()
    {
        QuestionnaireAnswers answers = Strong(horizonYears: 4, experience: ExperienceLevel.Intermediate);

        RiskScoring.Score(answers).Should().Be(RiskScoring.Score(answers));
    }
}
