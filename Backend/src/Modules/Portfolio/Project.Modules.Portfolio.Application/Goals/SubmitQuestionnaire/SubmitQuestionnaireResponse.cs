namespace Project.Modules.Portfolio.Application.Goals.SubmitQuestionnaire;

public sealed record SubmitQuestionnaireResponse(
    Guid GoalId,
    Guid ProfileId,
    int ProfileVersion,
    string ScoringVersion,
    int Capacity,
    int Tolerance,
    int EffectiveRisk,
    string RiskBand,
    bool SpeculativeUnlocked,
    string Engagement,
    string UsdComfort,
    decimal InvestmentAmount);
