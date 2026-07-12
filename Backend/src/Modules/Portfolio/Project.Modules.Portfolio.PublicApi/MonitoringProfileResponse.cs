namespace Project.Modules.Portfolio.PublicApi;

/// <summary>What the monitoring engine needs to pick tone and audience (§ 3.5):
/// the risk band, the goal framing, and how often the user wants to hear from us.
/// GoalType/Engagement are null for legacy users who never completed the
/// Phase 2 questionnaire.</summary>
public sealed record MonitoringProfileResponse(
    Guid UserId,
    string RiskProfile,
    string? GoalType,
    string? Engagement);
