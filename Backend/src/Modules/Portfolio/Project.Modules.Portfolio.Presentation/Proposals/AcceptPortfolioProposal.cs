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
using Project.Modules.Portfolio.Application.Proposals.AcceptProposal;

namespace Project.Modules.Portfolio.Presentation.Proposals;

internal sealed class AcceptPortfolioProposal : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/portfolio-proposals/{proposalId:guid}/accept", async (Guid proposalId, ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<PortfolioProposalResponse> result = await sender.Send(
                new AcceptPortfolioProposalCommand(claimsPrincipal.GetUserId(), proposalId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(AcceptPortfolioProposal))
        .WithSummary("Accept a portfolio proposal")
        .WithDescription("Marks the proposal as the goal's current accepted target and supersedes any previously accepted proposal. Idempotent; a superseded proposal cannot be re-accepted.")
        .Produces<PortfolioProposalResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Goals);
    }
}
