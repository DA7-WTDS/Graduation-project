namespace Project.Modules.Recommendations.Domain.DailyRuns;

/// <summary>
/// Kill-switch lifecycle for a daily run (IMPLEMENTATION_PLAN § 6.2).
/// Only <see cref="Published"/> runs are ever served to the optimizer or users.
/// </summary>
public enum DailyRunStatus
{
    /// <summary>Ingested clean but awaiting human approval (RequireManualApproval=true).</summary>
    PendingReview,

    /// <summary>Live: the run the optimizer and recommendation queries read.</summary>
    Published,

    /// <summary>Failed the pipeline's data-quality gates — persisted for audit only.</summary>
    Quarantined,

    /// <summary>Was published, then pulled by an operator after the fact.</summary>
    RolledBack,
}
