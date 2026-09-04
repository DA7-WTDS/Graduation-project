using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

/// <summary>Registers and schedules ShadowPortfolioJob with Quartz (nightly).</summary>
internal sealed class ConfigureShadowPortfolioJob(IOptions<ShadowPortfolioOptions> shadowOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly ShadowPortfolioOptions _shadowOptions = shadowOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(ShadowPortfolioJob).FullName!;

        options
            .AddJob<ShadowPortfolioJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithCronSchedule(_shadowOptions.CronSchedule,
                        x => x.WithMisfireHandlingInstructionFireAndProceed()));
    }
}
