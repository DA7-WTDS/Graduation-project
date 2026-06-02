using System.Text.Json.Serialization;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Recommendations.Application.Configuration;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;

namespace Project.Modules.Recommendations.Presentation.DailyRuns;

internal sealed class IngestDailyResults : IEndpoint
{
    private const string PipelineKeyHeader = "X-Pipeline-Key";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/daily-results",
            async (IngestRequest request, HttpContext http, IOptions<IngestOptions> ingestOptions, ISender sender) =>
        {
            // Machine-to-machine auth: validate the shared pipeline key (not a user JWT).
            string? expected = ingestOptions.Value.ApiKey;
            string? provided = http.Request.Headers[PipelineKeyHeader];
            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            Result<Guid> result = await sender.Send(
                new IngestDailyRunCommand(request.GeneratedAt, request.Records));

            return result.Match(
                runId => Results.Ok(new { runId, request.Count }),
                ApiResults.Problem);
        })
        .WithName(nameof(IngestDailyResults))
        .WithSummary("Ingest a daily prediction run from the pipeline")
        .WithDescription("Internal endpoint. The n8n pipeline POSTs the risk-graded daily run here, authenticated via the X-Pipeline-Key header.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithTags(Tags.Recommendations);
    }

    internal sealed record IngestRequest
    {
        [JsonPropertyName("generated_at")] public DateTime GeneratedAt { get; init; }
        [JsonPropertyName("count")] public int Count { get; init; }
        [JsonPropertyName("records")] public List<PredictionRecordDto> Records { get; init; } = [];
    }
}
