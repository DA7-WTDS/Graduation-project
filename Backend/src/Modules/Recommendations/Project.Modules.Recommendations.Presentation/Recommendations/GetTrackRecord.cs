using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Recommendations.Application.Recommendations.GetTrackRecord;

namespace Project.Modules.Recommendations.Presentation.Recommendations;

internal sealed class GetTrackRecord : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Deliberately anonymous: this feeds the public track-record page
        // (IMPLEMENTATION_PLAN § 5) — aggregates only, no user or position data.
        app.MapGet("/api/track-record", async (ISender sender) =>
        {
            Result<TrackRecordResponse> result = await sender.Send(new GetTrackRecordQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithName(nameof(GetTrackRecord))
        .WithSummary("Get realized prediction track record")
        .WithDescription("Rolling hit-rate and realized-return metrics computed from matured predictions (30-day horizon), overall and per risk level.")
        .Produces<TrackRecordResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Recommendations);
    }
}
