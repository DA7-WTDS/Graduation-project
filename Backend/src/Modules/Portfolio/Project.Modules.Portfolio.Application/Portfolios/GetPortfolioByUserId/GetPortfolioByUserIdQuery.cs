using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Portfolios.GetPortfolioByUserId;

public sealed record GetPortfolioByUserIdQuery(Guid UserId) : IQuery<PortfolioResponse>;
