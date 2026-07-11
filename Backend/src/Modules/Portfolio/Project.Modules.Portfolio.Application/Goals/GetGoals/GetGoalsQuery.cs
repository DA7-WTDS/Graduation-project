using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Goals.GetGoals;

public sealed record GetGoalsQuery(Guid UserId) : IQuery<IReadOnlyList<GoalResponse>>;
