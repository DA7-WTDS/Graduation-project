using FluentResults;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Portfolios.UpdatePortfolio;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Project.Modules.Portfolio.Presentation.Portfolios;

internal sealed class UpdatePortfolio : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/portfolios/{id}", async (ISender sender, Guid id, UpdatePortfolioRequest request) =>
        {
            Result result = await sender.Send(new UpdatePortfolioCommand(
                id,
                request.PrimaryGoal,
                request.TimeHorizon,
                request.RiskTolerance,
                request.MarketReaction,
                request.InvestmentExperience,
                request.StocksPercentage,
                request.BondsPercentage,
                request.EtfsPercentage,
                request.CashPercentage,
                request.RiskProfile,
                request.InvestmentAmount
            ));

            return result.Match(() => Results.NoContent(), ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(UpdatePortfolio))
        .WithSummary("Update portfolio")
        .WithDescription("Updates an existing portfolio's allocation and risk settings.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Portfolios);
    }

    internal sealed record UpdatePortfolioRequest(
        string PrimaryGoal,
        string TimeHorizon,
        int RiskTolerance,
        string MarketReaction,
        string InvestmentExperience,
        int StocksPercentage,
        int BondsPercentage,
        int EtfsPercentage,
        int CashPercentage,
        string RiskProfile,
        decimal InvestmentAmount);
}
