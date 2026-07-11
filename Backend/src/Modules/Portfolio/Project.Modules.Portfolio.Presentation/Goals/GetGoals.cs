using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Goals.GetGoals;

namespace Project.Modules.Portfolio.Presentation.Goals;

internal sealed class GetGoals : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals", async (ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<IReadOnlyList<GoalResponse>> result =
                await sender.Send(new GetGoalsQuery(claimsPrincipal.GetUserId()));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetGoals))
        .WithSummary("List the authenticated user's goals")
        .WithDescription("Returns each goal with its latest investor profile version (null if the questionnaire was never completed).")
        .Produces<IReadOnlyList<GoalResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Goals);
    }
}
