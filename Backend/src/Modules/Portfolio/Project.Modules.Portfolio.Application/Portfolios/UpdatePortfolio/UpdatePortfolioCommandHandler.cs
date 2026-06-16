using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using static Project.Modules.Portfolio.Domain.Portfolios.PortfolioErrors;

namespace Project.Modules.Portfolio.Application.Portfolios.UpdatePortfolio;

internal sealed class UpdatePortfolioCommandHandler(
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePortfolioCommand>
{
    public async Task<Result> Handle(UpdatePortfolioCommand request, CancellationToken cancellationToken)
    {
        Domain.Portfolios.Portfolio? portfolio = await portfolioRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (portfolio is null)
        {
            return Result.Fail(PortfolioNotFound(request.Id));
        }

        if (!Enum.TryParse<Domain.Portfolios.RiskProfile>(request.RiskProfile, out var riskProfile))
        {
            return Result.Fail(InvalidRiskProfile);
        }

        if (request.InvestmentAmount < 0)
        {
            return Result.Fail(InvalidInvestmentAmount(request.InvestmentAmount));
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
            riskProfile,
            request.InvestmentAmount
        );
        
        portfolioRepository.Update(portfolio, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Ok();
    }
}
