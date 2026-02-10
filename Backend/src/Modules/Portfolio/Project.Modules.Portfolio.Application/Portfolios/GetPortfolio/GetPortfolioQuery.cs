using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;

public sealed record GetPortfolioQuery(Guid PortfolioId) : IQuery<PortfolioResponse>;
