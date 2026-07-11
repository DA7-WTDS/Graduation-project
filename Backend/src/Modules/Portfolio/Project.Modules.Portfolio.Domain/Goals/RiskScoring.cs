using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Domain.Goals;

public sealed record RiskScore(
    int Capacity,
    int Tolerance,
    int EffectiveRisk,
    RiskProfile RiskBand,
    bool SpeculativeUnlocked);

/// <summary>
/// The versioned server-side scoring engine (§ 2.2). Deterministic and pure:
/// same answers ⇒ same score, forever, for a given <see cref="Version"/>.
///
/// Model: capacity (can this money take risk?) and tolerance (can this person
/// stomach risk?) are scored 0–100 independently; the effective risk is the
/// MINIMUM of the two — high willingness never overrides low ability, and a
/// cautious temperament never gets pushed into risk it doesn't want.
///
/// Any change to the weights below is a new Version string; old profiles keep
/// the version that produced them.
/// </summary>
public static class RiskScoring
{
    public const string Version = "v1";

    // Capacity gates: circumstances that cap risk-taking ability outright,
    // no matter how many points the other answers earn.
    private const int NoEmergencyFundCap = 35;
    private const int OversizedSavingsShareCap = 35;
    private const int NoIncomeCap = 50;

    // Speculative sleeve gate (§ 2.2): experience AND capacity AND explicit opt-in.
    private const int SpeculativeCapacityThreshold = 70;

    public static RiskScore Score(QuestionnaireAnswers a)
    {
        int capacity = ScoreCapacity(a);
        int tolerance = ScoreTolerance(a);
        int effective = Math.Min(capacity, tolerance);

        RiskProfile band = effective >= 70 ? RiskProfile.Aggressive
                         : effective >= 40 ? RiskProfile.Moderate
                         : RiskProfile.Conservative;

        bool speculative =
            a.Experience >= ExperienceLevel.Intermediate
            && capacity >= SpeculativeCapacityThreshold
            && a.AffordLossConfirmed;

        return new RiskScore(capacity, tolerance, effective, band, speculative);
    }

    private static int ScoreCapacity(QuestionnaireAnswers a)
    {
        int points = 0;

        points += a.HorizonYears switch
        {
            >= 10 => 40,
            >= 5 => 30,
            >= 3 => 20,
            >= 1 => 10,
            _ => 0
        };

        points += a.HasEmergencyFund ? 20 : 0;

        points += a.IncomeStability switch
        {
            IncomeStability.Stable => 20,
            IncomeStability.Variable => 10,
            _ => 0
        };

        points += a.SavingsShare switch
        {
            SavingsShareBand.LessThanTenPercent => 20,
            SavingsShareBand.TenToTwentyFivePercent => 15,
            SavingsShareBand.TwentyFiveToFiftyPercent => 5,
            _ => 0
        };

        if (!a.HasEmergencyFund)
        {
            points = Math.Min(points, NoEmergencyFundCap);
        }

        if (a.SavingsShare == SavingsShareBand.MoreThanFiftyPercent)
        {
            points = Math.Min(points, OversizedSavingsShareCap);
        }

        if (a.IncomeStability == IncomeStability.None)
        {
            points = Math.Min(points, NoIncomeCap);
        }

        return Math.Clamp(points, 0, 100);
    }

    private static int ScoreTolerance(QuestionnaireAnswers a)
    {
        int points = a.MarketReaction switch
        {
            MarketReactionAnswer.BuyMore => 50,
            MarketReactionAnswer.HoldSteady => 35,
            MarketReactionAnswer.SellSome => 15,
            _ => 0
        };

        points += a.Experience switch
        {
            ExperienceLevel.Experienced => 50,
            ExperienceLevel.Intermediate => 35,
            ExperienceLevel.Beginner => 15,
            _ => 5
        };

        return Math.Clamp(points, 0, 100);
    }
}
