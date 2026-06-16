using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Recommendations.Application.Recommendations.GetLatestPredictions;

namespace Project.Modules.Recommendations.Presentation.Recommendations;

internal sealed class GetLatestPredictions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/predictions", async (ISender sender) =>
        {
            Result<PredictionsResponse> result = await sender.Send(new GetLatestPredictionsQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetLatestPredictions))
        .WithSummary("Get latest market predictions")
        .WithDescription("Returns the raw per-ticker predictions from the latest pipeline run (market-wide, not personalized). Used by the learning environment.")
        .Produces<PredictionsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Recommendations);
    }
}
