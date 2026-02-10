using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Application.Portfolios.CreatePortfolio;

internal sealed class CreatePortfolioCommandHandler(
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePortfolioCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
    {
        Portfolio? existingPortfolio = await portfolioRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existingPortfolio is not null)
        {
            return Result.Fail(PortfolioErrors.PortfolioAlreadyExists(request.UserId));
        }

        if (!Enum.TryParse<RiskProfile>(request.RiskProfile, out var riskProfile))
        {
            return Result.Fail(PortfolioErrors.InvalidRiskProfile);
        }

        var portfolio = Portfolio.Create(
            request.UserId,
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

        await portfolioRepository.AddAsync(portfolio, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(portfolio.Id);
    }
}
