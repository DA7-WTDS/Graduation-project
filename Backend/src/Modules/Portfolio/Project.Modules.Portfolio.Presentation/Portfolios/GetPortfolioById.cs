using FluentResults;
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
        app.MapGet("/portfolios/{id}", async (ISender sender, Guid id) =>
        {
            Result<PortfolioResponse> result = await sender.Send(new GetPortfolioQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetPortfolioById))
        .WithSummary("Get portfolio by ID")
        .WithDescription("Retrieves a specific portfolio by its unique identifier.")
        .Produces<PortfolioResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Portfolios);
    }
}
