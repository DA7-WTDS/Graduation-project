using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Goals.GetPortfolioDraft;

namespace Project.Modules.Portfolio.Presentation.Goals;

internal sealed class GetPortfolioDraft : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals/{goalId:guid}/portfolio-draft", async (Guid goalId, ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<PortfolioDraftResponse> result = await sender.Send(
                new GetPortfolioDraftQuery(claimsPrincipal.GetUserId(), goalId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetPortfolioDraft))
        .WithSummary("Get a deterministic portfolio draft for a goal")
        .WithDescription("Selects the strategy template for the goal's latest investor profile and runs the allocation optimizer against the instrument registry and the latest daily rankings. Same inputs always produce the same draft (see inputsHash).")
        .Produces<PortfolioDraftResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Goals);
    }
}
