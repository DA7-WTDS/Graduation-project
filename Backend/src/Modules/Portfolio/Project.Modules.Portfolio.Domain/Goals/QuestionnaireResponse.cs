using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Goals;

/// <summary>
/// One questionnaire submission, stored verbatim (raw answers + the scoring version
/// that interpreted them). Append-only: retakes create new rows, nothing is edited.
/// This is the FRA suitability record.
/// </summary>
public sealed class QuestionnaireResponse : Entity
{
    private QuestionnaireResponse() { }

    public Guid Id { get; private set; }
    public Guid GoalId { get; private set; }
    public string AnswersJson { get; private set; }
    public string ScoringVersion { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    public static QuestionnaireResponse Create(Guid goalId, string answersJson, string scoringVersion)
    {
        return new QuestionnaireResponse
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            AnswersJson = answersJson,
            ScoringVersion = scoringVersion,
            SubmittedAt = DateTime.UtcNow
        };
    }
}
