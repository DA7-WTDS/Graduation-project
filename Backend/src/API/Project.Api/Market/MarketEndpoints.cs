using Project.Common.Presentation.Endpoints;

namespace Project.Api.Market;

/// <summary>
/// Market data proxy: symbol search + live quote, backed by <see cref="FinnhubClient"/>.
/// Authenticated — the Market page lives behind the app's private routes.
/// </summary>
internal sealed class MarketEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/market")
            .RequireAuthorization()
            .WithTags("Market");

        group.MapGet("/search", async (string? q, FinnhubClient client, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.Ok(Array.Empty<MarketSearchResult>());
            }

            IReadOnlyList<MarketSearchResult> results = await client.SearchAsync(q, ct);
            return Results.Ok(results);
        })
        .WithName("SearchMarketSymbols")
        .WithSummary("Search market symbols");

        group.MapGet("/quote", async (string? symbol, FinnhubClient client, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return Results.BadRequest(new { error = "symbol is required" });
            }

            MarketQuote? quote = await client.GetQuoteAsync(symbol, ct);
            return quote is null ? Results.NotFound() : Results.Ok(quote);
        })
        .WithName("GetMarketQuote")
        .WithSummary("Get a live quote for a symbol");
    }
}
