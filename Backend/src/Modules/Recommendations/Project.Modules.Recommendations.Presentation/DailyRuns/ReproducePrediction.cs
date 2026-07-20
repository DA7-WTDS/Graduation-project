using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Recommendations.Application.Configuration;
using Project.Modules.Recommendations.Application.DailyRuns.Reproduce;

namespace Project.Modules.Recommendations.Presentation.DailyRuns;

/// <summary>
/// § 6.3 audit endpoint: re-runs a stored prediction from its captured feature
/// vector and reports whether today's artifacts still produce what was served.
/// Internal (machine-key authed) — this is an audit and debugging tool.
/// </summary>
internal sealed class ReproducePrediction : IEndpoint
{
    private const string PipelineKeyHeader = "X-Pipeline-Key";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/predictions/{predictionId:guid}/reproduce",
            async (Guid predictionId, HttpContext http, IOptions<IngestOptions> ingestOptions, ISender sender) =>
        {
            string? expected = ingestOptions.Value.ApiKey;
            string? provided = http.Request.Headers[PipelineKeyHeader];
            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            Result<ReproducePredictionResponse> result =
                await sender.Send(new ReproducePredictionQuery(predictionId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithName(nameof(ReproducePrediction))
        .WithSummary("Re-run a stored prediction from its captured feature vector")
        .WithDescription(
            "Replays the exact scaled inputs through the pipeline's inference core and compares the " +
            "result with what was served. A differing model_version is reported, not rejected — " +
            "reproducing an old prediction under new artifacts is how you demonstrate what changed.")
        .Produces<ReproducePredictionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithTags(Tags.Recommendations);
    }
}
