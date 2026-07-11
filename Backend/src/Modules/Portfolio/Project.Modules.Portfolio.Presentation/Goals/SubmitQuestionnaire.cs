using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Goals.SubmitQuestionnaire;

namespace Project.Modules.Portfolio.Presentation.Goals;

internal sealed class SubmitQuestionnaire : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/goals/questionnaire", async (ISender sender, SubmitQuestionnaireRequest request, ClaimsPrincipal claimsPrincipal) =>
        {
            Result<SubmitQuestionnaireResponse> result = await sender.Send(new SubmitQuestionnaireCommand(
                claimsPrincipal.GetUserId(),
                request.GoalId,
                request.GoalType,
                request.HorizonYears,
                request.InvestmentAmount,
                request.MonthlyContribution,
                request.HasEmergencyFund,
                request.IncomeStability,
                request.SavingsShare,
                request.MarketReaction,
                request.Experience,
                request.Engagement,
                request.UsdComfort,
                request.AffordLossConfirmed));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(SubmitQuestionnaire))
        .WithSummary("Submit the onboarding questionnaire")
        .WithDescription("Stores the raw answers (append-only), scores them server-side into a versioned investor profile, and returns the derived risk profile and allocation. Pass goalId to retake for an existing goal.")
        .Produces<SubmitQuestionnaireResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithTags(Tags.Goals);
    }

    internal sealed record SubmitQuestionnaireRequest(
        Guid? GoalId,
        string GoalType,
        int HorizonYears,
        decimal InvestmentAmount,
        decimal MonthlyContribution,
        bool HasEmergencyFund,
        string IncomeStability,
        string SavingsShare,
        string MarketReaction,
        string Experience,
        string Engagement,
        string UsdComfort,
        bool AffordLossConfirmed);
}
