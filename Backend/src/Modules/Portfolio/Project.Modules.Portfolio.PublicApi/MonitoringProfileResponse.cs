namespace Project.Modules.Portfolio.PublicApi;

/// <summary>A user's investing profile as other modules see it: the scored risk
/// band, the goal it serves, and how often they want to hear from us. Sourced
/// from the latest versioned InvestorProfile (§ 2.2).</summary>
public sealed record MonitoringProfileResponse(
    Guid UserId,
    string RiskProfile,
    string GoalType,
    string Engagement);
