using System.Security.Claims;
using FluentResults;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Project.Modules.Portfolio.Presentation.Portfolios;

internal sealed class GetPortfolioById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/portfolios/{id}", async (ISender sender, ClaimsPrincipal claimsPrincipal, Guid id) =>
        {
            Result<PortfolioResponse> result = await sender.Send(new GetPortfolioQuery(id));

            // Only the owner may read a portfolio by id (prevents IDOR enumeration).
            if (result.IsSuccess && result.Value.UserId != claimsPrincipal.GetUserId())
            {
                return Results.Problem(title: "Forbidden", statusCode: StatusCodes.Status403Forbidden);
            }

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetPortfolioById))
        .WithSummary("Get portfolio by ID")
        .WithDescription("Retrieves a specific portfolio by its unique identifier (owner only).")
        .Produces<PortfolioResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Portfolios);
    }
}
