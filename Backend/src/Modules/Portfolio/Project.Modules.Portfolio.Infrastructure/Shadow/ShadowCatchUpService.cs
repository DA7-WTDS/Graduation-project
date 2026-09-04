using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

/// <summary>
/// Catches a missed nightly tick on startup (§ 6.1 ops reliability). If the host
/// was down at the 03:45 UTC cron and comes back up later in the day, the in-memory
/// scheduler would simply wait for tomorrow — leaving a gap in the track record.
///
/// On boot this checks: are we past today's tick time, and is there no snapshot for
/// today yet? If so, it fires the job once. The job is idempotent per UTC day
/// (SnapshotExistsAsync), so this is safe even when the scheduled run already ran.
/// </summary>
internal sealed class ShadowCatchUpService(
    IServiceScopeFactory scopeFactory,
    IOptions<ShadowPortfolioOptions> options,
    ILogger<ShadowCatchUpService> logger) : BackgroundService
{
    // The daily cron fires at 03:45 UTC; give the chain until 04:15 before we
    // consider a boot "after the tick" and worth catching up.
    private static readonly TimeOnly TickGrace = new(4, 15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the scheduler and DB settle before we probe.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            DateTime nowUtc = DateTime.UtcNow;
            if (TimeOnly.FromDateTime(nowUtc) < TickGrace)
            {
                logger.LogInformation("ShadowCatchUp — booted before today's tick window; nothing to catch up.");
                return;
            }

            string market = options.Value.Market;
            DateOnly today = DateOnly.FromDateTime(nowUtc);

            using IServiceScope scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IShadowPortfolioRepository>();

            IReadOnlyList<Domain.Shadow.ShadowSnapshot> snapshots = await repo.GetAllSnapshotsAsync(market, stoppingToken);
            bool haveToday = snapshots.Any(s => s.Date == today);
            if (haveToday)
            {
                logger.LogInformation("ShadowCatchUp — today's snapshot already exists; no catch-up needed.");
                return;
            }

            logger.LogWarning("ShadowCatchUp — no snapshot for {Today}; triggering a catch-up run.", today);
            var trigger = scope.ServiceProvider.GetRequiredService<IShadowRunTrigger>();
            await trigger.TriggerAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            // Never let a catch-up failure crash the host — the scheduled tick and
            // the manual endpoint remain as fallbacks.
            logger.LogError(ex, "ShadowCatchUp — failed to evaluate/trigger a catch-up run.");
        }
    }
}
