using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Infrastructure.Database;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Instruments;

/// <summary>
/// Nightly registry refresh (§ 3.1):
///   1. asks the pipeline for stats on its current universe — names that just
///      entered the screen auto-register as core-sleeve equities;
///   2. asks for stats on registry rows the universe request didn't cover
///      (curated ETFs/funds, names that dropped off the screen);
///   3. updates vol/liquidity/close on everything found.
/// Never deactivates anything by itself — that's a human decision until the
/// § 6.2 quality gates land.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class RefreshInstrumentStatsJob(
    HttpClient httpClient,
    PortfolioDbContext dbContext,
    IOptions<InstrumentsOptions> options,
    ILogger<RefreshInstrumentStatsJob> logger) : IJob
{
    private sealed record StatsRequest(List<string>? Tickers);

    private sealed record InstrumentStat(
        [property: JsonPropertyName("ticker")] string Ticker,
        [property: JsonPropertyName("realized_vol_1y")] double? RealizedVol1Y,
        [property: JsonPropertyName("avg_daily_value_traded")] double? AvgDailyValueTraded,
        [property: JsonPropertyName("last_close")] double? LastClose,
        [property: JsonPropertyName("sector")] string? Sector);

    private sealed record StatsResponse(
        [property: JsonPropertyName("market")] string Market,
        [property: JsonPropertyName("as_of")] DateTime AsOf,
        [property: JsonPropertyName("stats")] List<InstrumentStat> Stats);

    public async Task Execute(IJobExecutionContext context)
    {
        InstrumentsOptions opts = options.Value;
        CancellationToken ct = context.CancellationToken;

        StatsResponse? universe = await FetchAsync(null, ct);
        if (universe is null)
        {
            return; // logged inside; Quartz retries tomorrow
        }

        List<Instrument> registry = dbContext.Instruments
            .Where(i => i.Market == opts.Market)
            .ToList();
        var bySymbol = registry.ToDictionary(i => i.Symbol, StringComparer.OrdinalIgnoreCase);

        int registered = 0, refreshed = 0;

        foreach (InstrumentStat stat in universe.Stats)
        {
            if (bySymbol.TryGetValue(stat.Ticker, out Instrument? existing))
            {
                existing.UpdateStats(stat.RealizedVol1Y, stat.AvgDailyValueTraded, stat.LastClose, stat.Sector, universe.AsOf);
                refreshed++;
            }
            else
            {
                // New screened equity → core sleeve. Curated ETFs/funds are seeded
                // by migration; only stocks ever auto-register.
                var instrument = Instrument.Create(
                    opts.Market, stat.Ticker, InstrumentType.Stock, AssetClass.Equity, "USD",
                    [Sleeves.Core], stat.Sector);
                instrument.UpdateStats(stat.RealizedVol1Y, stat.AvgDailyValueTraded, stat.LastClose, stat.Sector, universe.AsOf);
                dbContext.Instruments.Add(instrument);
                bySymbol[stat.Ticker] = instrument;
                registered++;
            }
        }

        // Registry rows the universe call didn't cover (ETFs, dropped names).
        var covered = universe.Stats.Select(s => s.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> leftovers = registry
            .Where(i => i.IsActive && !covered.Contains(i.Symbol))
            .Select(i => i.Symbol)
            .ToList();

        if (leftovers.Count > 0)
        {
            StatsResponse? extra = await FetchAsync(leftovers, ct);
            if (extra is not null)
            {
                foreach (InstrumentStat stat in extra.Stats)
                {
                    if (bySymbol.TryGetValue(stat.Ticker, out Instrument? instrument))
                    {
                        instrument.UpdateStats(stat.RealizedVol1Y, stat.AvgDailyValueTraded, stat.LastClose, stat.Sector, extra.AsOf);
                        refreshed++;
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "RefreshInstrumentStatsJob — done. Registered={Registered}, Refreshed={Refreshed}, RegistrySize={Size}.",
            registered, refreshed, registry.Count + registered);
    }

    private async Task<StatsResponse?> FetchAsync(List<string>? tickers, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/instrument-stats", new StatsRequest(tickers), ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StatsResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RefreshInstrumentStatsJob — /api/instrument-stats failed ({Scope}).",
                tickers is null ? "universe" : $"{tickers.Count} registry symbols");
            return null;
        }
    }
}
