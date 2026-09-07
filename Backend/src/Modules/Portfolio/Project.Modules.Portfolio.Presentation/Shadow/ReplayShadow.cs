using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Project.Common.Presentation.Endpoints;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;

namespace Project.Modules.Portfolio.Presentation.Shadow;

/// <summary>
/// Walks the model portfolios across a date range (§ C fidelity lane).
///
/// Sequential by necessity, not convenience: each session's return is measured against
/// the previous session's NAV, so replaying out of order produces a curve that never
/// existed. Firing the Quartz job per date cannot give that ordering — it is
/// fire-and-forget — which is why the run body was extracted into IShadowRunner.
///
/// Per date, in this order: refresh the registry AS OF that date (so positions are
/// weighted by the volatility and traded value that existed then, not today's), then
/// value the portfolios against that date's run.
/// </summary>
internal sealed class ReplayShadow : IEndpoint
{
    private const string PipelineKeyHeader = "X-Pipeline-Key";

    internal sealed record ReplayRequest(DateOnly From, DateOnly To, bool Simulated = true, bool RefreshStats = true);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/shadow/replay",
            async (ReplayRequest request, HttpContext http, IConfiguration config,
                   IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory) =>
        {
            string? expected = config["Recommendations:Ingest:ApiKey"];
            string? provided = http.Request.Headers[PipelineKeyHeader];
            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            if (request.To < request.From)
            {
                return Results.Problem(title: "`to` is before `from`.", statusCode: StatusCodes.Status400BadRequest);
            }

            ILogger logger = loggerFactory.CreateLogger<ReplayShadow>();
            int sessions = 0, valued = 0, rebalanced = 0, skipped = 0;

            for (DateOnly d = request.From; d <= request.To; d = d.AddDays(1))
            {
                // A FRESH scope per session, exactly as each Quartz fire gets in
                // production. Sharing one DbContext across the whole range grows the
                // change tracker without bound and breaks on the second rebalance: EF
                // re-issues the DELETE for position rows it already removed, surfacing
                // as "expected to affect 1 row, actually affected 0". Isolating each
                // session is also the more faithful replay.
                using IServiceScope scope = scopeFactory.CreateScope();
                var refresher = scope.ServiceProvider.GetRequiredService<IInstrumentStatsRefresher>();
                var runner = scope.ServiceProvider.GetRequiredService<IShadowRunner>();

                if (request.RefreshStats)
                {
                    await refresher.RefreshAsync(d, null, http.RequestAborted);
                }

                ShadowRunOutcome outcome;
                try
                {
                    outcome = await runner.RunAsync(d, request.Simulated, http.RequestAborted);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Name the offending entities rather than surfacing EF's generic
                    // "0 rows affected", which says nothing about what went wrong.
                    string detail = string.Join(" | ", ex.Entries.Select(e =>
                        $"{e.Entity.GetType().Name}/{e.State} keys=[" +
                        string.Join(",", e.Properties.Where(p => p.Metadata.IsPrimaryKey() || p.Metadata.IsForeignKey())
                            .Select(p => $"{p.Metadata.Name}={p.CurrentValue}")) + "]"));
                    logger.LogError(ex, "Shadow replay failed on {Date}. Entries: {Detail}", d, detail);
                    return Results.Problem(
                        title: $"Replay failed on {d:yyyy-MM-dd}: {detail}",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
                sessions++;
                valued += outcome.Valued;
                rebalanced += outcome.Rebalanced;
                skipped += outcome.Skipped;

                if (sessions % 20 == 0)
                {
                    logger.LogInformation("Shadow replay — {Sessions} sessions, {Valued} valuations so far ({Date}).",
                        sessions, valued, d);
                }
            }

            logger.LogInformation("Shadow replay — done. {Sessions} sessions, {Valued} valued, {Rebalanced} rebalanced, {Skipped} skipped.",
                sessions, valued, rebalanced, skipped);

            return Results.Ok(new { sessions, valued, rebalanced, skipped });
        })
        .WithName(nameof(ReplayShadow))
        .WithSummary("Replay the model portfolios across a date range")
        .WithDescription("Internal. Walks each session in order, refreshing point-in-time registry stats before valuing, so a replayed portfolio is weighted by the data that existed then.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithTags(Tags.Portfolios);
    }
}
