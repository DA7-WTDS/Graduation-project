using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Instruments;

/// <summary>Registers and schedules RefreshInstrumentStatsJob with Quartz (nightly).</summary>
internal sealed class ConfigureRefreshInstrumentStatsJob(IOptions<InstrumentsOptions> instrumentsOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly InstrumentsOptions _instrumentsOptions = instrumentsOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(RefreshInstrumentStatsJob).FullName!;

        options
            .AddJob<RefreshInstrumentStatsJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    // Fire-and-proceed: a tick missed during downtime runs once on
                    // recovery instead of being silently skipped (§ ops reliability).
                    .WithCronSchedule(_instrumentsOptions.CronSchedule,
                        x => x.WithMisfireHandlingInstructionFireAndProceed()));
    }
}
