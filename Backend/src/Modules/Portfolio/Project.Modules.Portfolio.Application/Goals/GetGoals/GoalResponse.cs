namespace Project.Modules.Portfolio.Application.Goals.GetGoals;

public sealed record GoalProfileResponse(
    Guid ProfileId,
    int Version,
    string ScoringVersion,
    int Capacity,
    int Tolerance,
    int EffectiveRisk,
    string RiskBand,
    string Engagement,
    string UsdComfort,
    bool SpeculativeUnlocked,
    DateTime CreatedAt);

public sealed record GoalResponse(
    Guid Id,
    string Type,
    int HorizonYears,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    GoalProfileResponse? Profile);
