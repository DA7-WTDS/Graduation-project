using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Goals;

/// <summary>
/// A user's investment goal ("what is this money for?"). Portfolios, questionnaire
/// responses and investor profiles all hang off a goal, not the user, so one user
/// can eventually run several goals with different risk postures.
/// </summary>
public sealed class Goal : Entity
{
    private Goal() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public GoalType Type { get; private set; }
    public int HorizonYears { get; private set; }

    /// <summary>The capital earmarked for this goal — what the optimizer sizes
    /// positions against. Lives here (not in the answers blob) because it is a
    /// queryable fact about the goal, not just a questionnaire response.</summary>
    public decimal InvestmentAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Goal Create(Guid userId, GoalType type, int horizonYears, decimal investmentAmount)
    {
        return new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            HorizonYears = horizonYears,
            InvestmentAmount = investmentAmount,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Retaking the questionnaire may change the goal's framing; the goal
    /// row is updated but responses/profiles are never mutated (append-only).</summary>
    public void Redefine(GoalType type, int horizonYears, decimal investmentAmount)
    {
        Type = type;
        HorizonYears = horizonYears;
        InvestmentAmount = investmentAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}
