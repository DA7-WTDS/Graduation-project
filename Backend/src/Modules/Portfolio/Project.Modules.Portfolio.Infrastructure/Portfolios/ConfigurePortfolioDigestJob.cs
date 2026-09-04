using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

/// <summary>Registers and schedules PortfolioDigestJob with Quartz (daily check).</summary>
internal sealed class ConfigurePortfolioDigestJob(IOptions<PortfolioDigestOptions> digestOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly PortfolioDigestOptions _digestOptions = digestOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(PortfolioDigestJob).FullName!;

        options
            .AddJob<PortfolioDigestJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithCronSchedule(_digestOptions.CronSchedule,
                        x => x.WithMisfireHandlingInstructionFireAndProceed()));
    }
}
