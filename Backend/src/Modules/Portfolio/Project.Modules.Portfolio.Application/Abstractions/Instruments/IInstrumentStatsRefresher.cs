namespace Project.Modules.Portfolio.Application.Abstractions.Instruments;

/// <summary>
/// Refreshes the instrument registry's computed stats (vol, traded value, close).
///
/// <paramref name="asOf"/> makes the refresh point-in-time. That matters because the
/// optimizer weights every core position by score / realized_vol, caps sectors, and
/// gates the tactical sleeve on traded value: replaying a 2025 portfolio against 2026
/// volatility is lookahead, not a labelling detail. Null means "now", the live
/// nightly behaviour.
/// </summary>
public interface IInstrumentStatsRefresher
{
    Task<InstrumentRefreshResult> RefreshAsync(
        DateOnly? asOf = null,
        IReadOnlyList<string>? tickers = null,
        CancellationToken cancellationToken = default);
}

public sealed record InstrumentRefreshResult(int Registered, int Refreshed, int RegistrySize);
