using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

namespace Project.Modules.Recommendations.Presentation.Recommendations;

internal sealed class GetRecommendations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/recommendations", async (ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<RecommendationResponse> result = await sender.Send(
                new GetRecommendationsQuery(claimsPrincipal.GetUserId()));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetRecommendations))
        .WithSummary("Get personalized AI recommendations")
        .WithDescription("Returns LLM-generated recommendations from the latest prediction run, personalized to the authenticated user's risk profile.")
        .Produces<RecommendationResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Recommendations);
    }
}
