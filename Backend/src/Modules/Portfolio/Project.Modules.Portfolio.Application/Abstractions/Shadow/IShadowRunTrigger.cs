namespace Project.Modules.Portfolio.Application.Abstractions.Shadow;

/// <summary>
/// Runs the nightly shadow-portfolio job on demand (§ 6.1 ops). Lets the internal
/// endpoint and startup catch-up fire the same Quartz job without the Presentation
/// layer taking a dependency on Quartz or the job type.
/// </summary>
public interface IShadowRunTrigger
{
    /// <param name="runDate">Session to value. Null = today (the nightly tick);
    /// a past date replays that session (§ C fidelity lane).</param>
    /// <param name="simulated">Read Simulated runs instead of Published ones.</param>
    Task TriggerAsync(
        DateOnly? runDate = null, bool simulated = false, CancellationToken cancellationToken = default);
}
