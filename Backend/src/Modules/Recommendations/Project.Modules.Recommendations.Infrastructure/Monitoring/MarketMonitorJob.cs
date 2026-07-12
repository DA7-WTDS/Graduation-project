using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Common.Application.EventBus;
using Project.Modules.Recommendations.Domain.Monitoring;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Recommendations.IntegrationEvents;
using Quartz;
using System.Net.Http.Json;

namespace Project.Modules.Recommendations.Infrastructure.Monitoring;

/// <summary>
/// Nightly trigger evaluation (§ 3.5) — triggers, not schedules: the job runs
/// every night but events fire only when a rule crosses its threshold.
///   • Market crash: index down ≥ X% over the trailing window (fires on the
///     crossing night only — MonitorRules compares today's window to yesterday's).
///   • Conviction reversal: a held name newly flipped to DOWN + NEGATIVE
///     between the two latest runs. One event per affected user.
/// Notifications turns these events into per-profile messages. Drawdown and
/// allocation-drift triggers arrive with Phase 4 (they need portfolio valuations).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class MarketMonitorJob(
    HttpClient httpClient,
    RecommendationsDbContext dbContext,
    IEventBus eventBus,
    IOptions<MonitorOptions> options,
    ILogger<MarketMonitorJob> logger) : IJob
{
    private sealed record ClosesRequest(List<string> Tickers, string Start, string End);

    private sealed record ClosesResponse(string Market, Dictionary<string, Dictionary<string, double>> Closes);

    public async Task Execute(IJobExecutionContext context)
    {
        MonitorOptions opts = options.Value;
        CancellationToken ct = context.CancellationToken;

        await CheckMarketCrashAsync(opts, ct);
        await CheckConvictionReversalsAsync(ct);
    }

    private async Task CheckMarketCrashAsync(MonitorOptions opts, CancellationToken ct)
    {
        // Calendar padding: windowDays trading days + weekends/holidays + the
        // extra day MonitorRules needs to see "yesterday's window".
        var request = new ClosesRequest(
            Tickers: [opts.IndexTicker],
            Start: DateTime.UtcNow.Date.AddDays(-(opts.CrashWindowDays * 2 + 12)).ToString("yyyy-MM-dd"),
            End: DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd"));

        Dictionary<string, double>? series;
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/closes", request, ct);
            response.EnsureSuccessStatusCode();
            ClosesResponse? closes = await response.Content.ReadFromJsonAsync<ClosesResponse>(cancellationToken: ct);
            series = closes?.Closes.GetValueOrDefault(opts.IndexTicker);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MarketMonitorJob — index closes fetch failed; crash check skipped tonight.");
            return;
        }

        if (series is null || series.Count == 0)
        {
            logger.LogWarning("MarketMonitorJob — no closes for {Index}; crash check skipped.", opts.IndexTicker);
            return;
        }

        List<double> ordered = series
            .OrderBy(kv => DateTime.Parse(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        (bool crossedToday, double currentDrop) = MonitorRules.CrashCrossed(ordered, opts.CrashWindowDays, opts.CrashDropPct);

        if (crossedToday)
        {
            logger.LogWarning("MARKET CRASH TRIGGER — {Index} {Drop:P1} over {Window} trading days.",
                opts.IndexTicker, currentDrop, opts.CrashWindowDays);
            await eventBus.PublishAsync(new MarketCrashDetectedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, opts.IndexTicker, currentDrop, opts.CrashWindowDays, DateTime.UtcNow.Date), ct);
        }
        else
        {
            logger.LogInformation("Crash check — {Index} {Drop:P1} over {Window}d (threshold −{Threshold:P0}); no crossing.",
                opts.IndexTicker, currentDrop, opts.CrashWindowDays, opts.CrashDropPct);
        }
    }

    private async Task CheckConvictionReversalsAsync(CancellationToken ct)
    {
        var lastRuns = await dbContext.DailyRuns
            .AsNoTracking()
            .OrderByDescending(r => r.GeneratedAt)
            .Take(2)
            .Select(r => new { r.Id, r.GeneratedAt })
            .ToListAsync(ct);

        if (lastRuns.Count < 2)
        {
            logger.LogInformation("Reversal check — fewer than two runs ingested; skipped.");
            return;
        }

        Dictionary<string, (string Direction, string Signal)> latest = await LoadRunSignalsAsync(lastRuns[0].Id, ct);
        Dictionary<string, (string Direction, string Signal)> previous = await LoadRunSignalsAsync(lastRuns[1].Id, ct);

        var holdingsByUser = (await dbContext.UserHoldings
                .AsNoTracking()
                .Select(h => new { h.UserId, h.Ticker })
                .ToListAsync(ct))
            .GroupBy(h => h.UserId);

        int events = 0;
        foreach (var user in holdingsByUser)
        {
            IReadOnlyList<string> reversals = MonitorRules.NewReversals(
                user.Select(h => h.Ticker), latest, previous);

            if (reversals.Count > 0)
            {
                await eventBus.PublishAsync(new ConvictionReversalDetectedIntegrationEvent(
                    Guid.NewGuid(), DateTime.UtcNow, user.Key, reversals.ToList(), lastRuns[0].GeneratedAt), ct);
                events++;
            }
        }

        logger.LogInformation("Reversal check — {Events} user(s) with newly flipped holdings.", events);
    }

    private async Task<Dictionary<string, (string Direction, string Signal)>> LoadRunSignalsAsync(Guid runId, CancellationToken ct)
    {
        var rows = await dbContext.StockPredictions
            .AsNoTracking()
            .Where(p => p.DailyRunId == runId)
            .Select(p => new { p.Ticker, p.Direction, p.Signal })
            .ToListAsync(ct);

        return rows.ToDictionary(
            r => r.Ticker,
            r => (r.Direction, r.Signal),
            StringComparer.OrdinalIgnoreCase);
    }
}
