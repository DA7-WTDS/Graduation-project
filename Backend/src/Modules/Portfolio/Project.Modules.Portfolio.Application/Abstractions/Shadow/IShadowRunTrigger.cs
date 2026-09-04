namespace Project.Modules.Portfolio.Application.Abstractions.Shadow;

/// <summary>
/// Runs the nightly shadow-portfolio job on demand (§ 6.1 ops). Lets the internal
/// endpoint and startup catch-up fire the same Quartz job without the Presentation
/// layer taking a dependency on Quartz or the job type.
/// </summary>
public interface IShadowRunTrigger
{
    Task TriggerAsync(CancellationToken cancellationToken = default);
}
