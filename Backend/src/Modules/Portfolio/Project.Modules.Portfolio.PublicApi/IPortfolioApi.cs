namespace Project.Modules.Portfolio.PublicApi;

/// <summary>
/// What other modules may know about a user's investing profile. Everything here
/// is derived from the goal + versioned investor profile (Phase 2) — the legacy
/// single-portfolio row is gone.
/// </summary>
public interface IPortfolioApi
{
    /// <summary>Risk band + goal framing + engagement for one user, or null if
    /// they have not completed the questionnaire.</summary>
    Task<MonitoringProfileResponse?> GetMonitoringProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Every user who has completed onboarding (has a scored profile).
    /// Used to fan out daily/market-wide notifications.</summary>
    Task<IReadOnlyList<Guid>> GetProfiledUserIdsAsync(CancellationToken cancellationToken = default);
}
