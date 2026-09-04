using Microsoft.Extensions.Options;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

/// <summary>Registers and schedules PortfolioValuationJob with Quartz (nightly).</summary>
internal sealed class ConfigurePortfolioValuationJob(IOptions<PortfolioValuationOptions> valuationOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly PortfolioValuationOptions _valuationOptions = valuationOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(PortfolioValuationJob).FullName!;

        options
            .AddJob<PortfolioValuationJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithCronSchedule(_valuationOptions.CronSchedule,
                        x => x.WithMisfireHandlingInstructionFireAndProceed()));
    }
}
