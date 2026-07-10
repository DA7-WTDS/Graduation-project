using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Recommendations.Infrastructure.Outcomes;

/// <summary>Registers and schedules ScoreOutcomesJob with Quartz (nightly).</summary>
internal sealed class ConfigureScoreOutcomesJob(IOptions<OutcomesOptions> outcomesOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly OutcomesOptions _outcomesOptions = outcomesOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(ScoreOutcomesJob).FullName!;

        options
            .AddJob<ScoreOutcomesJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithCronSchedule(_outcomesOptions.CronSchedule));
    }
}
