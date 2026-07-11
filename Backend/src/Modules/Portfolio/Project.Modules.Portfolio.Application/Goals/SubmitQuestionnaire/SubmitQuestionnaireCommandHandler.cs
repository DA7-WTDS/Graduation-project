using System.Text.Json;
using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Portfolios;
using static Project.Modules.Portfolio.Domain.Goals.GoalErrors;

namespace Project.Modules.Portfolio.Application.Goals.SubmitQuestionnaire;

internal sealed class SubmitQuestionnaireCommandHandler(
    IGoalRepository goalRepository,
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitQuestionnaireCommand, SubmitQuestionnaireResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<Result<SubmitQuestionnaireResponse>> Handle(
        SubmitQuestionnaireCommand request, CancellationToken cancellationToken)
    {
        Result<QuestionnaireAnswers> parsed = ParseAnswers(request);
        if (parsed.IsFailed)
        {
            return Result.Fail<SubmitQuestionnaireResponse>(parsed.Errors);
        }

        QuestionnaireAnswers answers = parsed.Value;

        // First submission creates the goal; a retake redefines it and appends
        // a new response + profile version. Nothing is ever overwritten.
        Goal? goal = null;
        if (request.GoalId is Guid goalId)
        {
            goal = await goalRepository.GetByIdAsync(goalId, cancellationToken);
            if (goal is null)
            {
                return Result.Fail(GoalNotFound(goalId));
            }

            if (goal.UserId != request.UserId)
            {
                return Result.Fail(UnauthorizedAccess);
            }

            goal.Redefine(answers.GoalType, answers.HorizonYears);
        }
        else
        {
            goal = Goal.Create(request.UserId, answers.GoalType, answers.HorizonYears);
            await goalRepository.AddGoalAsync(goal, cancellationToken);
        }

        var response = QuestionnaireResponse.Create(
            goal.Id,
            JsonSerializer.Serialize(answers, JsonOptions),
            RiskScoring.Version);
        await goalRepository.AddResponseAsync(response, cancellationToken);

        RiskScore score = RiskScoring.Score(answers);

        InvestorProfile? previous = await goalRepository.GetLatestProfileAsync(goal.Id, cancellationToken);
        var profile = InvestorProfile.Create(
            goal.Id,
            response.Id,
            (previous?.Version ?? 0) + 1,
            score,
            answers.Engagement,
            answers.UsdComfort,
            RiskScoring.Version);
        await goalRepository.AddProfileAsync(profile, cancellationToken);

        // Legacy bridge: until the Phase 3 optimizer builds portfolios from
        // templates, derive the coarse allocation the dashboard and the
        // recommendation engine already consume — server-side now, not in React.
        (int stocks, int bonds, int etfs, int cash) = AllocationFor(score.RiskBand);
        Domain.Portfolios.Portfolio portfolio = await UpsertLegacyPortfolioAsync(
            request, answers, score, stocks, bonds, etfs, cash, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new SubmitQuestionnaireResponse(
            goal.Id,
            profile.Id,
            profile.Version,
            profile.ScoringVersion,
            score.Capacity,
            score.Tolerance,
            score.EffectiveRisk,
            score.RiskBand.ToString(),
            score.SpeculativeUnlocked,
            answers.Engagement.ToString(),
            answers.UsdComfort.ToString(),
            portfolio.Id,
            stocks, bonds, etfs, cash));
    }

    private static Result<QuestionnaireAnswers> ParseAnswers(SubmitQuestionnaireCommand r)
    {
        if (r.HorizonYears is < 0 or > 60)
        {
            return Result.Fail(InvalidHorizon(r.HorizonYears));
        }

        if (r.InvestmentAmount < 0)
        {
            return Result.Fail(InvalidAmount(r.InvestmentAmount));
        }

        if (r.MonthlyContribution < 0)
        {
            return Result.Fail(InvalidAmount(r.MonthlyContribution));
        }

        if (!TryParse(r.GoalType, out GoalType goalType))
        {
            return Result.Fail(InvalidAnswer("goal type", r.GoalType, Allowed<GoalType>()));
        }

        if (!TryParse(r.IncomeStability, out IncomeStability income))
        {
            return Result.Fail(InvalidAnswer("income stability", r.IncomeStability, Allowed<IncomeStability>()));
        }

        if (!TryParse(r.SavingsShare, out SavingsShareBand savings))
        {
            return Result.Fail(InvalidAnswer("savings share", r.SavingsShare, Allowed<SavingsShareBand>()));
        }

        if (!TryParse(r.MarketReaction, out MarketReactionAnswer reaction))
        {
            return Result.Fail(InvalidAnswer("market reaction", r.MarketReaction, Allowed<MarketReactionAnswer>()));
        }

        if (!TryParse(r.Experience, out ExperienceLevel experience))
        {
            return Result.Fail(InvalidAnswer("experience", r.Experience, Allowed<ExperienceLevel>()));
        }

        if (!TryParse(r.Engagement, out EngagementLevel engagement))
        {
            return Result.Fail(InvalidAnswer("engagement", r.Engagement, Allowed<EngagementLevel>()));
        }

        if (!TryParse(r.UsdComfort, out UsdComfort usdComfort))
        {
            return Result.Fail(InvalidAnswer("usd comfort", r.UsdComfort, Allowed<UsdComfort>()));
        }

        return Result.Ok(new QuestionnaireAnswers(
            goalType,
            r.HorizonYears,
            r.InvestmentAmount,
            r.MonthlyContribution,
            r.HasEmergencyFund,
            income,
            savings,
            reaction,
            experience,
            engagement,
            usdComfort,
            r.AffordLossConfirmed));
    }

    private static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct, Enum =>
        Enum.TryParse(value?.Replace("_", ""), ignoreCase: true, out result) && Enum.IsDefined(result);

    private static string Allowed<TEnum>() where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetNames<TEnum>());

    private static (int Stocks, int Bonds, int Etfs, int Cash) AllocationFor(RiskProfile band) => band switch
    {
        RiskProfile.Aggressive => (60, 20, 15, 5),
        RiskProfile.Moderate => (40, 35, 20, 5),
        _ => (20, 50, 25, 5)
    };

    private async Task<Domain.Portfolios.Portfolio> UpsertLegacyPortfolioAsync(
        SubmitQuestionnaireCommand request,
        QuestionnaireAnswers answers,
        RiskScore score,
        int stocks, int bonds, int etfs, int cash,
        CancellationToken cancellationToken)
    {
        string timeHorizon = answers.HorizonYears switch
        {
            < 1 => "short",
            <= 2 => "medium",
            _ => "long"
        };

        Domain.Portfolios.Portfolio? existing =
            await portfolioRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(
                answers.GoalType.ToString(),
                timeHorizon,
                score.EffectiveRisk,
                answers.MarketReaction.ToString(),
                answers.Experience.ToString(),
                stocks, bonds, etfs, cash,
                score.RiskBand,
                answers.InvestmentAmount);
            return existing;
        }

        var portfolio = Domain.Portfolios.Portfolio.Create(
            request.UserId,
            answers.GoalType.ToString(),
            timeHorizon,
            score.EffectiveRisk,
            answers.MarketReaction.ToString(),
            answers.Experience.ToString(),
            stocks, bonds, etfs, cash,
            score.RiskBand,
            answers.InvestmentAmount);
        await portfolioRepository.AddAsync(portfolio, cancellationToken);
        return portfolio;
    }
}
