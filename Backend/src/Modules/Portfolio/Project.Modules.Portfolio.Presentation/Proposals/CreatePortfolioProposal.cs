using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Proposals;
using Project.Modules.Portfolio.Application.Proposals.CreateProposal;

namespace Project.Modules.Portfolio.Presentation.Proposals;

internal sealed class CreatePortfolioProposal : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/goals/{goalId:guid}/proposals", async (Guid goalId, ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<PortfolioProposalResponse> result = await sender.Send(
                new CreatePortfolioProposalCommand(claimsPrincipal.GetUserId(), goalId));

            return result.Match(
                proposal => Results.Created($"/api/portfolio-proposals/{proposal.Id}", proposal),
                ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(CreatePortfolioProposal))
        .WithSummary("Generate and persist a portfolio proposal for a goal")
        .WithDescription("Runs the deterministic optimizer against current registry + rankings and stores the result as the next immutable proposal version. The draft endpoint previews the same computation without persisting.")
        .Produces<PortfolioProposalResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Goals);
    }
}
