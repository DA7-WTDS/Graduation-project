using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Domain.Goals;

namespace Project.Modules.Portfolio.Application.Goals.GetGoals;

internal sealed class GetGoalsQueryHandler(IGoalRepository goalRepository)
    : IQueryHandler<GetGoalsQuery, IReadOnlyList<GoalResponse>>
{
    public async Task<Result<IReadOnlyList<GoalResponse>>> Handle(
        GetGoalsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Goal> goals = await goalRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var responses = new List<GoalResponse>(goals.Count);
        foreach (Goal goal in goals)
        {
            InvestorProfile? profile = await goalRepository.GetLatestProfileAsync(goal.Id, cancellationToken);
            responses.Add(new GoalResponse(
                goal.Id,
                goal.Type.ToString(),
                goal.HorizonYears,
                goal.CreatedAt,
                goal.UpdatedAt,
                profile is null ? null : new GoalProfileResponse(
                    profile.Id,
                    profile.Version,
                    profile.ScoringVersion,
                    profile.Capacity,
                    profile.Tolerance,
                    profile.EffectiveRisk,
                    profile.RiskBand.ToString(),
                    profile.Engagement.ToString(),
                    profile.UsdComfort.ToString(),
                    profile.SpeculativeUnlocked,
                    profile.CreatedAt)));
        }

        return Result.Ok<IReadOnlyList<GoalResponse>>(responses);
    }
}
