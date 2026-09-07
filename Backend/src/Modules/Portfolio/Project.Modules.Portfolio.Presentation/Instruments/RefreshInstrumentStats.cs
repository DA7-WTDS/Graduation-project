using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Project.Common.Presentation.Endpoints;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;

namespace Project.Modules.Portfolio.Presentation.Instruments;

/// <summary>
/// Point-in-time registry refresh, for the § C replay driver.
///
/// The driver calls this before pushing each replayed date so the optimizer weights
/// that date's portfolio by the volatility and traded value that existed THEN. Without
/// it the replay inherits today's numbers, which is lookahead: across the replayed
/// universe ~23% of names moved vol by more than 30% over the window, and the largest
/// by 1.5-2.2x.
///
/// Machine-key authed, same internal key as the other /api/internal routes.
/// </summary>
internal sealed class RefreshInstrumentStats : IEndpoint
{
    private const string PipelineKeyHeader = "X-Pipeline-Key";

    internal sealed record Request(string? Market, DateOnly? AsOf, List<string>? Tickers);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/instrument-stats",
            async (Request request, HttpContext http, IConfiguration config, IInstrumentStatsRefresher refresher) =>
        {
            string? expected = config["Recommendations:Ingest:ApiKey"];
            string? provided = http.Request.Headers[PipelineKeyHeader];
            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            InstrumentRefreshResult result = await refresher.RefreshAsync(
                request.AsOf, request.Tickers, http.RequestAborted);

            return Results.Ok(new { result.Registered, result.Refreshed, result.RegistrySize, asOf = request.AsOf });
        })
        .WithName(nameof(RefreshInstrumentStats))
        .WithSummary("Refresh instrument registry stats, optionally as of a past date")
        .WithDescription("Internal. Omit asOf for today's values (the nightly behaviour); supply it to make vol, traded value and close point-in-time for a replayed date.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithTags(Tags.Portfolios);
    }
}
