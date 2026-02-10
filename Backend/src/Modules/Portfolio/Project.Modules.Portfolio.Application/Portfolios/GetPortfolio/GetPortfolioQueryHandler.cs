using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;

internal sealed class GetPortfolioQueryHandler(IPortfolioRepository portfolioRepository)
    : IQueryHandler<GetPortfolioQuery, PortfolioResponse>
{
    public async Task<Result<PortfolioResponse>> Handle(GetPortfolioQuery request, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = await portfolioRepository.GetByIdAsync(request.PortfolioId, cancellationToken);
        
        if (portfolio is null)
        {
            return Result.Fail(PortfolioErrors.PortfolioNotFound(request.PortfolioId));
        }
        
        return Result.Ok(new PortfolioResponse(
            portfolio.Id,
            portfolio.UserId,
            portfolio.PrimaryGoal,
            portfolio.TimeHorizon,
            portfolio.RiskTolerance,
            portfolio.MarketReaction,
            portfolio.InvestmentExperience,
            portfolio.StocksPercentage,
            portfolio.BondsPercentage,
            portfolio.EtfsPercentage,
            portfolio.CashPercentage,
            portfolio.RiskProfile.ToString(),
            portfolio.CreatedAt,
            portfolio.UpdatedAt
        ));
    }
}
