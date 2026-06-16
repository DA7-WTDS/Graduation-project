using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Project.Api.Market;

/// <summary>A live quote for a single symbol (normalized from Finnhub's terse shape).</summary>
public sealed record MarketQuote(
    string Symbol,
    decimal Current,
    decimal Change,
    decimal PercentChange,
    decimal High,
    decimal Low,
    decimal Open,
    decimal PreviousClose);

/// <summary>A symbol-search hit.</summary>
public sealed record MarketSearchResult(string Symbol, string Description, string Type);

/// <summary>
/// Thin server-side proxy over the Finnhub REST API. Keeps the API key out of the
/// browser and gives the frontend a clean, normalized contract (see /api/market/*).
/// </summary>
public sealed class FinnhubClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<MarketSearchResult>> SearchAsync(string query, CancellationToken ct)
    {
        FinnhubSearchResponse? response = await httpClient.GetFromJsonAsync<FinnhubSearchResponse>(
            $"/api/v1/search?q={Uri.EscapeDataString(query)}", ct);

        if (response?.Result is null)
        {
            return [];
        }

        return response.Result
            .Where(r => !string.IsNullOrWhiteSpace(r.Symbol) && !r.Symbol.Contains('.')) // drop foreign-suffixed dups
            .Select(r => new MarketSearchResult(r.Symbol, r.Description ?? r.Symbol, r.Type ?? string.Empty))
            .Take(15)
            .ToList();
    }

    public async Task<MarketQuote?> GetQuoteAsync(string symbol, CancellationToken ct)
    {
        FinnhubQuote? q = await httpClient.GetFromJsonAsync<FinnhubQuote>(
            $"/api/v1/quote?symbol={Uri.EscapeDataString(symbol)}", ct);

        // Finnhub returns all-zero / null for an unknown symbol on the free tier.
        if (q?.Current is null or 0)
        {
            return null;
        }

        return new MarketQuote(
            symbol.ToUpperInvariant(),
            q.Current ?? 0,
            q.Change ?? 0,
            q.PercentChange ?? 0,
            q.High ?? 0,
            q.Low ?? 0,
            q.Open ?? 0,
            q.PreviousClose ?? 0);
    }

    private sealed record FinnhubSearchResponse(int Count, List<FinnhubSearchItem>? Result);

    private sealed record FinnhubSearchItem(string Symbol, string? Description, string? Type);

    private sealed record FinnhubQuote(
        [property: JsonPropertyName("c")] decimal? Current,
        [property: JsonPropertyName("d")] decimal? Change,
        [property: JsonPropertyName("dp")] decimal? PercentChange,
        [property: JsonPropertyName("h")] decimal? High,
        [property: JsonPropertyName("l")] decimal? Low,
        [property: JsonPropertyName("o")] decimal? Open,
        [property: JsonPropertyName("pc")] decimal? PreviousClose);
}
