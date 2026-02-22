using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;

namespace Project.Modules.Portfolio.Application.Portfolios.GetPortfolioByUserId;

public sealed record GetPortfolioByUserIdQuery(Guid UserId) : IQuery<PortfolioResponse>;
