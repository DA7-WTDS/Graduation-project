using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Recommendations.Infrastructure.Pipeline;

/// <summary>
/// Registers and schedules FetchDailyPipelineJob with Quartz.
/// Uses a cron trigger (daily, Tue–Sat at 01:00 UTC) — same cadence as the
/// previous n8n Schedule Trigger node (cron: 0 1 * * 2-6 Africa/Cairo ≈ 23:00 UTC prev day,
/// but running at 01:00 UTC captures each Mon–Fri US session ~2h after the 21:00 UTC close).
/// </summary>
internal sealed class ConfigureFetchDailyPipelineJob(IOptions<PipelineOptions> pipelineOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly PipelineOptions _pipelineOptions = pipelineOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(FetchDailyPipelineJob).FullName!;

        options
            .AddJob<FetchDailyPipelineJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithCronSchedule(_pipelineOptions.CronSchedule));
    }
}
