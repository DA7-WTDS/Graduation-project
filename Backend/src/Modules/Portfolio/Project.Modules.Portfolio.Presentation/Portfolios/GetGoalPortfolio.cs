using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Portfolios.GetGoalPortfolio;

namespace Project.Modules.Portfolio.Presentation.Portfolios;

internal sealed class GetGoalPortfolio : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals/{goalId:guid}/portfolio", async (Guid goalId, ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<GoalPortfolioResponse> result = await sender.Send(
                new GetGoalPortfolioQuery(claimsPrincipal.GetUserId(), goalId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetGoalPortfolio))
        .WithSummary("Get the goal's live accepted portfolio")
        .WithDescription("Marks the accepted portfolio to market against the registry's latest closes: NAV, drawdown from the high-water mark, total return, per-position target vs actual weights, and the next review date. 404 until a proposal is accepted.")
        .Produces<GoalPortfolioResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithTags(Tags.Goals);
    }
}
