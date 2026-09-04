using Quartz;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

/// <summary>
/// Fires the existing <see cref="ShadowPortfolioJob"/> immediately via Quartz, so
/// an on-demand run reuses the exact same code path as the scheduled tick. The job
/// is idempotent per UTC day, so an extra run never double-counts.
/// </summary>
internal sealed class ShadowRunTrigger(ISchedulerFactory schedulerFactory) : IShadowRunTrigger
{
    // Matches the identity in ConfigureShadowPortfolioJob (typeof(...).FullName).
    private static readonly JobKey JobKey = new(typeof(ShadowPortfolioJob).FullName!);

    public async Task TriggerAsync(CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.TriggerJob(JobKey, cancellationToken);
    }
}
