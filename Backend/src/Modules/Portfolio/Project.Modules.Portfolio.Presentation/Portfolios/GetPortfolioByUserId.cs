using System.Security.Claims;
using FluentResults;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolioByUserId;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Project.Modules.Portfolio.Presentation.Portfolios;

internal sealed class GetMyPortfolio : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/portfolios/me", async (ISender sender, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<PortfolioResponse> result = await sender.Send(
                new GetPortfolioByUserIdQuery(claimsPrincipal.GetUserId()));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetMyPortfolio")
        .WithSummary("Get my portfolio")
        .WithDescription("Retrieves the portfolio for the currently authenticated user.")
        .Produces<PortfolioResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Portfolios);
    }
}
