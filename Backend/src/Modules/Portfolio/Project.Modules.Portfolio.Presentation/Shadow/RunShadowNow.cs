using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Project.Common.Presentation.Endpoints;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;

namespace Project.Modules.Portfolio.Presentation.Shadow;

/// <summary>
/// § 6.1 ops: run the nightly shadow-portfolio job on demand — for testing, or to
/// catch up a missed tick without waiting for 03:45 UTC. Machine-key authed
/// (same X-Pipeline-Key as ingest); the job is idempotent per UTC day.
/// </summary>
internal sealed class RunShadowNow : IEndpoint
{
    private const string PipelineKeyHeader = "X-Pipeline-Key";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/shadow/run",
            async (HttpContext http, IConfiguration config, IShadowRunTrigger trigger) =>
        {
            // Shared internal machine key (the pipeline/ops key), not a user JWT.
            string? expected = config["Recommendations:Ingest:ApiKey"];
            string? provided = http.Request.Headers[PipelineKeyHeader];
            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            await trigger.TriggerAsync(http.RequestAborted);
            return Results.Accepted(value: new { status = "triggered" });
        })
        .WithName(nameof(RunShadowNow))
        .WithSummary("Run the shadow-portfolio job now")
        .WithDescription("Fires the nightly model-portfolio run on demand (idempotent per UTC day). Internal, X-Pipeline-Key authed.")
        .Produces<object>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithTags(Tags.Portfolios);
    }
}
