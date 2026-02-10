using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Application.Portfolios.GetPortfolioByUserId;

internal sealed class GetPortfolioByUserIdQueryHandler(IPortfolioRepository portfolioRepository)
    : IQueryHandler<GetPortfolioByUserIdQuery, PortfolioResponse>
{
    public async Task<Result<PortfolioResponse>> Handle(GetPortfolioByUserIdQuery request, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = await portfolioRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        if (portfolio is null)
        {
            return Result.Fail(PortfolioErrors.PortfolioNotFoundForUser(request.UserId));
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
