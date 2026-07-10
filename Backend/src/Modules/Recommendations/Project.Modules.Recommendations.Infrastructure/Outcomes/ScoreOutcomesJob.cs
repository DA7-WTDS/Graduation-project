using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Modules.Recommendations.Domain.Outcomes;
using Project.Modules.Recommendations.Infrastructure.Database;
using Quartz;
using System.Net.Http.Json;

namespace Project.Modules.Recommendations.Infrastructure.Outcomes;

/// <summary>
/// Nightly realized-outcome scorer (IMPLEMENTATION_PLAN § 0.3 — the feedback loop).
///
/// For every StockPrediction whose horizon has elapsed and that has no outcome yet:
///   1. fetch historical closes from the pipeline's POST /api/closes
///      (baseline window around the run date, realized window around run date + horizon)
///   2. compute realized return + direction hit
///   3. persist an immutable PredictionOutcome row.
///
/// Idempotent: the unique index on StockPredictionId plus the "no outcome yet"
/// query filter mean re-runs only ever fill gaps. Predictions whose realized
/// close is not available yet (holidays, data gaps) are skipped and retried
/// the next night.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class ScoreOutcomesJob(
    HttpClient httpClient,
    RecommendationsDbContext dbContext,
    IOptions<OutcomesOptions> options,
    ILogger<ScoreOutcomesJob> logger) : IJob
{
    private sealed record PendingPrediction(
        Guid Id, string Ticker, string Direction, double ChangePct, string RiskLevel, DateTime GeneratedAt);

    private sealed record ClosesRequest(List<string> Tickers, string Start, string End);

    private sealed record ClosesResponse(string Market, Dictionary<string, Dictionary<string, double>> Closes);

    public async Task Execute(IJobExecutionContext context)
    {
        OutcomesOptions opts = options.Value;

        // horizon + 1 day so the realized close (first trading day >= target) can exist.
        DateTime cutoff = DateTime.UtcNow.AddDays(-(opts.HorizonDays + 1));

        List<PendingPrediction> pending = await (
            from p in dbContext.StockPredictions
            join r in dbContext.DailyRuns on p.DailyRunId equals r.Id
            where r.GeneratedAt <= cutoff
               && !dbContext.PredictionOutcomes.Any(o => o.StockPredictionId == p.Id)
            orderby r.GeneratedAt
            select new PendingPrediction(p.Id, p.Ticker, p.Direction, p.ChangePct, p.RiskLevel, r.GeneratedAt))
            .Take(opts.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (pending.Count == 0)
        {
            logger.LogInformation("ScoreOutcomesJob — nothing matured to score.");
            return;
        }

        logger.LogInformation("ScoreOutcomesJob — scoring {Count} matured predictions.", pending.Count);

        int scored = 0, skipped = 0;

        // One closes request per run-date group (same window for all its tickers).
        foreach (var group in pending.GroupBy(p => p.GeneratedAt.Date))
        {
            DateTime runDate = group.Key;
            DateTime targetDate = runDate.AddDays(opts.HorizonDays);

            var request = new ClosesRequest(
                Tickers: group.Select(p => p.Ticker).Distinct().ToList(),
                Start: runDate.AddDays(-7).ToString("yyyy-MM-dd"),
                End: targetDate.AddDays(8).ToString("yyyy-MM-dd"));

            ClosesResponse? closes;
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    "/api/closes", request, context.CancellationToken);
                response.EnsureSuccessStatusCode();
                closes = await response.Content.ReadFromJsonAsync<ClosesResponse>(
                    cancellationToken: context.CancellationToken);
            }
            catch (Exception ex)
            {
                // Log and continue with other groups — Quartz retries tomorrow.
                logger.LogError(ex, "ScoreOutcomesJob — /api/closes failed for run {RunDate:yyyy-MM-dd}.", runDate);
                continue;
            }

            if (closes is null || closes.Closes.Count == 0)
            {
                logger.LogWarning("ScoreOutcomesJob — empty closes for run {RunDate:yyyy-MM-dd}.", runDate);
                continue;
            }

            foreach (PendingPrediction p in group)
            {
                if (!closes.Closes.TryGetValue(p.Ticker, out Dictionary<string, double>? series) || series.Count == 0)
                {
                    skipped++;
                    continue; // delisted/renamed or data gap — retried, then stays pending
                }

                var byDate = series
                    .Select(kv => (Date: DateTime.Parse(kv.Key), Close: kv.Value))
                    .OrderBy(x => x.Date)
                    .ToList();

                // Entry: last close at/before the run date. Exit: first close at/after target.
                var baseline = byDate.LastOrDefault(x => x.Date <= runDate);
                var realized = byDate.FirstOrDefault(x => x.Date >= targetDate);

                if (baseline.Close <= 0 || realized.Close <= 0)
                {
                    skipped++;
                    continue;
                }

                dbContext.PredictionOutcomes.Add(PredictionOutcome.Create(
                    p.Id, p.Ticker, p.GeneratedAt, p.Direction, p.ChangePct, p.RiskLevel,
                    opts.HorizonDays, baseline.Close, realized.Close));
                scored++;
            }

            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        logger.LogInformation(
            "ScoreOutcomesJob — done. Scored={Scored}, Skipped={Skipped} (no usable closes).",
            scored, skipped);

        await CheckDriftAsync(opts, context.CancellationToken);
    }

    /// <summary>Drift alarm (IMPLEMENTATION_PLAN § 1.7): the model is retrained
    /// monthly, but if live accuracy degrades between retrains someone must know.</summary>
    private async Task CheckDriftAsync(OutcomesOptions opts, CancellationToken ct)
    {
        DateTime windowStart = DateTime.UtcNow.AddDays(-90);
        var window = await dbContext.PredictionOutcomes
            .Where(o => o.RunGeneratedAt >= windowStart)
            .Select(o => o.DirectionHit)
            .ToListAsync(ct);

        if (window.Count < opts.DriftMinSamples)
        {
            return;
        }

        double hitRate = window.Count(h => h) / (double)window.Count;
        if (hitRate < opts.DriftWarnHitRate)
        {
            logger.LogWarning(
                "MODEL DRIFT ALARM — rolling 90d hit-rate {HitRate:P1} over {Count} outcomes is below the {Threshold:P0} floor. Investigate before the next retrain.",
                hitRate, window.Count, opts.DriftWarnHitRate);
        }
        else
        {
            logger.LogInformation(
                "Drift check — rolling 90d hit-rate {HitRate:P1} over {Count} outcomes (floor {Threshold:P0}).",
                hitRate, window.Count, opts.DriftWarnHitRate);
        }
    }
}
