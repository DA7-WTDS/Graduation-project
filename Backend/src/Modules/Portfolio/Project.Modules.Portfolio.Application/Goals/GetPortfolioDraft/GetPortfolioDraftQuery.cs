using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Goals.GetPortfolioDraft;

public sealed record GetPortfolioDraftQuery(Guid UserId, Guid GoalId) : IQuery<PortfolioDraftResponse>;
