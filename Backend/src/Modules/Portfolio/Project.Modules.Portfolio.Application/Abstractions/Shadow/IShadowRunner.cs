namespace Project.Modules.Portfolio.Application.Abstractions.Shadow;

/// <summary>
/// Values the model portfolios for one session.
///
/// Extracted from the Quartz job so a date range can be replayed IN ORDER. Firing the
/// job per date through the scheduler is fire-and-forget: the caller races ahead and
/// sessions land out of sequence, which corrupts a NAV series that is inherently
/// sequential (each day's return is measured against the previous day's NAV).
/// </summary>
public interface IShadowRunner
{
    Task<ShadowRunOutcome> RunAsync(
        DateOnly runDate, bool simulated = false, CancellationToken cancellationToken = default);
}

public sealed record ShadowRunOutcome(int Valued, int Rebalanced, int Skipped, int DrawdownAlerts);
