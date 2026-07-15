using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Portfolios.GetGoalPortfolio;

/// <summary>The goal's live (accepted) portfolio, marked to market.</summary>
public sealed record GetGoalPortfolioQuery(Guid UserId, Guid GoalId) : IQuery<GoalPortfolioResponse>;
