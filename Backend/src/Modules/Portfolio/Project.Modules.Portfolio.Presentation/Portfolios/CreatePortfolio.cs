using System.Security.Claims;
using FluentResults;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Portfolios.CreatePortfolio;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Project.Modules.Portfolio.Presentation.Portfolios;

internal sealed class CreatePortfolio : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/portfolios", async (ISender sender, CreatePortfolioRequest request, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<Guid> result = await sender.Send(new CreatePortfolioCommand(
                claimsPrincipal.GetUserId(),
                request.PrimaryGoal,
                request.TimeHorizon,
                request.RiskTolerance,
                request.MarketReaction,
                request.InvestmentExperience,
                request.StocksPercentage,
                request.BondsPercentage,
                request.EtfsPercentage,
                request.CashPercentage,
                request.RiskProfile
            ));

            return result.Match(
                portfolioId => Results.Created($"/api/portfolios/{portfolioId}", new { Id = portfolioId }),
                ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(CreatePortfolio))
        .WithSummary("Create a portfolio")
        .WithDescription("Creates a new portfolio for the authenticated user based on the onboarding questionnaire.")
        .Produces<object>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Portfolios);
    }

    internal sealed record CreatePortfolioRequest(
        string PrimaryGoal,
        string TimeHorizon,
        int RiskTolerance,
        string MarketReaction,
        string InvestmentExperience,
        int StocksPercentage,
        int BondsPercentage,
        int EtfsPercentage,
        int CashPercentage,
        string RiskProfile);
}
