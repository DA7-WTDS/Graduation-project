using Microsoft.Extensions.Logging;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

/// <summary>
/// The nightly tick (§ 6.1). The work lives in <see cref="IShadowRunner"/>, shared with
/// the § C replay so a manufactured track record is produced by the same code that
/// values live portfolios.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class ShadowPortfolioJob(
    IShadowRunner runner,
    ILogger<ShadowPortfolioJob> logger) : IJob
{
    internal const string RunDateKey = "runDate";
    internal const string SimulatedKey = "simulated";

    public async Task Execute(IJobExecutionContext context)
    {
        DateOnly runDate = context.MergedJobDataMap.TryGetString(RunDateKey, out string? raw)
                        && DateOnly.TryParse(raw, out DateOnly parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);
        bool simulated = context.MergedJobDataMap.GetBooleanValueFromString(SimulatedKey);

        ShadowRunOutcome outcome = await runner.RunAsync(runDate, simulated, context.CancellationToken);

        logger.LogInformation(
            "ShadowPortfolioJob[{RunDate}] — valued {Valued}, rebalanced {Rebalanced}, skipped {Skipped}.",
            runDate, outcome.Valued, outcome.Rebalanced, outcome.Skipped);
    }
}
