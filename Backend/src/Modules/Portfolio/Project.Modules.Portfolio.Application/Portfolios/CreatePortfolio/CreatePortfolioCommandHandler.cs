using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using static Project.Modules.Portfolio.Domain.Portfolios.PortfolioErrors;

namespace Project.Modules.Portfolio.Application.Portfolios.CreatePortfolio;

internal sealed class CreatePortfolioCommandHandler(
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePortfolioCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
    {
        Domain.Portfolios.Portfolio? existingPortfolio = await portfolioRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existingPortfolio is not null)
        {
            return Result.Fail(PortfolioAlreadyExists(request.UserId));
        }

        if (!Enum.TryParse<Domain.Portfolios.RiskProfile>(request.RiskProfile, out var riskProfile))
        {
            return Result.Fail(InvalidRiskProfile);
        }

        if (request.InvestmentAmount < 0)
        {
            return Result.Fail(InvalidInvestmentAmount(request.InvestmentAmount));
        }

        var portfolio = Domain.Portfolios.Portfolio.Create(
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
            riskProfile,
            request.InvestmentAmount
        ); // All fields populated at creation — domain bug fixed

        await portfolioRepository.AddAsync(portfolio, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(portfolio.Id);
    }
}
