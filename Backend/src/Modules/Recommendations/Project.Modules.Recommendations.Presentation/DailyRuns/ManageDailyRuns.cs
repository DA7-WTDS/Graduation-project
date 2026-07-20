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
using Project.Modules.Recommendations.Application.DailyRuns.GetRecent;
using Project.Modules.Recommendations.Application.DailyRuns.UpdateStatus;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Presentation.DailyRuns;

/// <summary>
/// § 6.2 kill-switch operator endpoints. Machine-key authenticated like the
/// ingest endpoint — these are internal ops tools, not user-facing surface:
///   GET  /api/internal/daily-runs                — recent runs with status
///   POST /api/internal/daily-runs/{id}/status    — flip a run's lifecycle state
/// </summary>
internal sealed class ManageDailyRuns : IEndpoint
{
    private const string PipelineKeyHeader = "X-Pipeline-Key";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/internal/daily-runs",
            async (HttpContext http, IOptions<IngestOptions> ingestOptions, ISender sender) =>
        {
            if (!IsAuthorized(http, ingestOptions))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            Result<IReadOnlyList<DailyRunStatusResponse>> result =
                await sender.Send(new GetRecentDailyRunsQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithName("GetRecentDailyRuns")
        .WithSummary("List recent daily runs with their kill-switch status")
        .Produces<IReadOnlyList<DailyRunStatusResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithTags(Tags.Recommendations);

        app.MapPost("/api/internal/daily-runs/{runId:guid}/status",
            async (Guid runId, UpdateStatusRequest request, HttpContext http,
                   IOptions<IngestOptions> ingestOptions, ISender sender) =>
        {
            if (!IsAuthorized(http, ingestOptions))
            {
                return Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized);
            }

            if (!TryParseStatus(request.Status, out DailyRunStatus target))
            {
                return Results.Problem(
                    title: $"Unknown status '{request.Status}'. Expected one of: pending_review, published, quarantined, rolled_back.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            Result<DailyRunStatusResponse> result = await sender.Send(
                new UpdateDailyRunStatusCommand(runId, target, request.Reason));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithName("UpdateDailyRunStatus")
        .WithSummary("Flip a daily run's kill-switch status (publish / quarantine / roll back)")
        .Produces<DailyRunStatusResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithTags(Tags.Recommendations);
    }

    private static bool IsAuthorized(HttpContext http, IOptions<IngestOptions> ingestOptions)
    {
        string? expected = ingestOptions.Value.ApiKey;
        string? provided = http.Request.Headers[PipelineKeyHeader];
        return !string.IsNullOrWhiteSpace(expected) && string.Equals(provided, expected, StringComparison.Ordinal);
    }

    private static bool TryParseStatus(string? raw, out DailyRunStatus status)
    {
        status = default;
        string normalized = (raw ?? string.Empty).Replace("_", string.Empty).Trim();
        if (Enum.TryParse(normalized, ignoreCase: true, out DailyRunStatus parsed) &&
            Enum.IsDefined(parsed))
        {
            status = parsed;
            return true;
        }

        return false;
    }

    internal sealed record UpdateStatusRequest
    {
        /// <summary>pending_review | published | quarantined | rolled_back.</summary>
        [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
        [JsonPropertyName("reason")] public string? Reason { get; init; }
    }
}
