using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Strategies;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Instruments;

/// <summary>
/// The registry refresh, shared by the nightly job and the § C replay driver.
///
/// One implementation on purpose: the replay must populate the registry through the
/// same code the live system uses, or the manufactured track record is measuring a
/// second implementation rather than the product.
/// </summary>
internal sealed class InstrumentStatsRefresher(
    HttpClient httpClient,
    PortfolioDbContext dbContext,
    IOptions<InstrumentsOptions> options,
    ILogger<InstrumentStatsRefresher> logger) : IInstrumentStatsRefresher
{
    private sealed record StatsRequest(
        [property: JsonPropertyName("tickers")] List<string>? Tickers,
        [property: JsonPropertyName("as_of")] string? AsOf);

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

    public async Task<InstrumentRefreshResult> RefreshAsync(
        DateOnly? asOf = null,
        IReadOnlyList<string>? tickers = null,
        CancellationToken cancellationToken = default)
    {
        InstrumentsOptions opts = options.Value;

        StatsResponse? universe = await FetchAsync(tickers?.ToList(), asOf, cancellationToken);
        if (universe is null)
        {
            return new InstrumentRefreshResult(0, 0, 0);
        }

        List<Instrument> registry = await dbContext.Instruments
            .Where(i => i.Market == opts.Market)
            .ToListAsync(cancellationToken);
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
                // New screened equity — core sleeve. Curated ETFs/funds are seeded by
                // migration; only stocks ever auto-register.
                var instrument = Instrument.Create(
                    opts.Market, stat.Ticker, InstrumentType.Stock, AssetClass.Equity, "USD",
                    [Sleeves.Core], stat.Sector);
                instrument.UpdateStats(stat.RealizedVol1Y, stat.AvgDailyValueTraded, stat.LastClose, stat.Sector, universe.AsOf);
                dbContext.Instruments.Add(instrument);
                bySymbol[stat.Ticker] = instrument;
                registered++;
            }
        }

        // Registry rows the first call did not cover (ETFs, names off the screen).
        // Skipped when the caller named its own tickers: a replay asks for exactly the
        // universe it replayed, and topping that up with today's ETF list would put
        // instruments into the book that the replayed run never ranked.
        if (tickers is null)
        {
            var covered = universe.Stats.Select(s => s.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<string> leftovers = registry
                .Where(i => i.IsActive && !covered.Contains(i.Symbol))
                .Select(i => i.Symbol)
                .ToList();

            if (leftovers.Count > 0)
            {
                StatsResponse? extra = await FetchAsync(leftovers, asOf, cancellationToken);
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
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Instrument registry refresh{AsOf} — registered {Registered}, refreshed {Refreshed}, size {Size}.",
            asOf is null ? "" : $" as of {asOf:yyyy-MM-dd}", registered, refreshed, registry.Count + registered);

        return new InstrumentRefreshResult(registered, refreshed, registry.Count + registered);
    }

    private async Task<StatsResponse?> FetchAsync(
        List<string>? tickers, DateOnly? asOf, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/instrument-stats",
                new StatsRequest(tickers, asOf?.ToString("yyyy-MM-dd")),
                cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StatsResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Instrument registry refresh — /api/instrument-stats failed ({Scope}).",
                tickers is null ? "universe" : $"{tickers.Count} tickers");
            return null;
        }
    }
}
