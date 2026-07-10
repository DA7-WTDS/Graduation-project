namespace Project.Modules.Recommendations.Application.Abstractions.Outcomes;

/// <summary>Read access to realized prediction outcomes (IMPLEMENTATION_PLAN § 0.3).</summary>
public interface IPredictionOutcomeRepository
{
    /// <summary>All outcomes for runs generated at/after <paramref name="since"/>.</summary>
    Task<IReadOnlyList<OutcomeStat>> GetSinceAsync(DateTime since, CancellationToken cancellationToken = default);
}

/// <summary>Slim projection used for rolling metrics — no entity tracking.</summary>
public sealed record OutcomeStat(
    DateTime RunGeneratedAt,
    string RiskLevel,
    bool DirectionHit,
    double RealizedReturnPct);
