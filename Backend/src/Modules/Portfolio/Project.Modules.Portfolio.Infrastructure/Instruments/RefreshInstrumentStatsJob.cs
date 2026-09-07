using Microsoft.Extensions.Logging;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Instruments;

/// <summary>
/// Nightly registry refresh (§ 3.1). The work itself lives in
/// <see cref="IInstrumentStatsRefresher"/>, shared with the § C replay driver so a
/// manufactured track record is built by the same code that serves live.
///
/// Never deactivates anything by itself — that stays a human decision.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class RefreshInstrumentStatsJob(
    IInstrumentStatsRefresher refresher,
    ILogger<RefreshInstrumentStatsJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // No asOf: the nightly job wants today's numbers.
        InstrumentRefreshResult result = await refresher.RefreshAsync(
            cancellationToken: context.CancellationToken);

        logger.LogInformation(
            "RefreshInstrumentStatsJob — done. Registered={Registered}, Refreshed={Refreshed}, RegistrySize={Size}.",
            result.Registered, result.Refreshed, result.RegistrySize);
    }
}
