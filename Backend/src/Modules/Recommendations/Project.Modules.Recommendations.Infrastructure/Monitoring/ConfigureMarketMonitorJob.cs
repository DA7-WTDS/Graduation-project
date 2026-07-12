using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Recommendations.Infrastructure.Monitoring;

/// <summary>Registers and schedules MarketMonitorJob with Quartz (nightly).</summary>
internal sealed class ConfigureMarketMonitorJob(IOptions<MonitorOptions> monitorOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly MonitorOptions _monitorOptions = monitorOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(MarketMonitorJob).FullName!;

        options
            .AddJob<MarketMonitorJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithCronSchedule(_monitorOptions.CronSchedule));
    }
}
