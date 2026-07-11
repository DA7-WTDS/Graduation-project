using Project.Common.Domain.Abstractions;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Domain.Goals;

/// <summary>
/// The derived risk profile for a goal at a point in time. Append-only: every
/// questionnaire submission produces a new version; the highest version is current.
/// </summary>
public sealed class InvestorProfile : Entity
{
    private InvestorProfile() { }

    public Guid Id { get; private set; }
    public Guid GoalId { get; private set; }
    public Guid QuestionnaireResponseId { get; private set; }
    public int Version { get; private set; }

    public int Capacity { get; private set; }
    public int Tolerance { get; private set; }
    public int EffectiveRisk { get; private set; }
    public RiskProfile RiskBand { get; private set; }

    public EngagementLevel Engagement { get; private set; }
    public UsdComfort UsdComfort { get; private set; }
    public bool SpeculativeUnlocked { get; private set; }

    public string ScoringVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static InvestorProfile Create(
        Guid goalId,
        Guid questionnaireResponseId,
        int version,
        RiskScore score,
        EngagementLevel engagement,
        UsdComfort usdComfort,
        string scoringVersion)
    {
        return new InvestorProfile
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            QuestionnaireResponseId = questionnaireResponseId,
            Version = version,
            Capacity = score.Capacity,
            Tolerance = score.Tolerance,
            EffectiveRisk = score.EffectiveRisk,
            RiskBand = score.RiskBand,
            Engagement = engagement,
            UsdComfort = usdComfort,
            SpeculativeUnlocked = score.SpeculativeUnlocked,
            ScoringVersion = scoringVersion,
            CreatedAt = DateTime.UtcNow
        };
    }
}
