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
using Project.Modules.Portfolio.Application.Proposals.GetProposals;

namespace Project.Modules.Portfolio.Presentation.Proposals;

internal sealed class GetPortfolioProposals : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals/{goalId:guid}/proposals", async (Guid goalId, ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<IReadOnlyList<PortfolioProposalResponse>> result = await sender.Send(
                new GetPortfolioProposalsQuery(claimsPrincipal.GetUserId(), goalId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetPortfolioProposals))
        .WithSummary("List a goal's portfolio proposals")
        .WithDescription("Returns every proposal generated for the goal, newest version first, with its status (Proposed/Accepted/Superseded).")
        .Produces<IReadOnlyList<PortfolioProposalResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithTags(Tags.Goals);
    }
}
