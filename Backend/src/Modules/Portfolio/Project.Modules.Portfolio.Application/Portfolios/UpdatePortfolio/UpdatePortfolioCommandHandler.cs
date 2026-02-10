using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Application.Portfolios.UpdatePortfolio;

internal sealed class UpdatePortfolioCommandHandler(
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePortfolioCommand>
{
    public async Task<Result> Handle(UpdatePortfolioCommand request, CancellationToken cancellationToken)
    {
        Portfolio? portfolio = await portfolioRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (portfolio is null)
        {
            return Result.Fail(PortfolioErrors.PortfolioNotFound(request.Id));
        }

        if (!Enum.TryParse<RiskProfile>(request.RiskProfile, out var riskProfile))
        {
            return Result.Fail(PortfolioErrors.InvalidRiskProfile);
        }
        
        portfolio.Update(
            request.PrimaryGoal,
            request.TimeHorizon,
            request.RiskTolerance,
            request.MarketReaction,
            request.InvestmentExperience,
            request.StocksPercentage,
            request.BondsPercentage,
            request.EtfsPercentage,
            request.CashPercentage,
            riskProfile
        );
        
        portfolioRepository.Update(portfolio, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Ok();
    }
}
