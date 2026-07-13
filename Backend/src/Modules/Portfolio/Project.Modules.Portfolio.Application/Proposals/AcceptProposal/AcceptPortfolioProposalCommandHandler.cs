using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Allocation;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Proposals;
using static Project.Modules.Portfolio.Domain.Proposals.ProposalErrors;

namespace Project.Modules.Portfolio.Application.Proposals.AcceptProposal;

internal sealed class AcceptPortfolioProposalCommandHandler(
    IPortfolioProposalRepository proposalRepository,
    IGoalRepository goalRepository,
    IGoalPortfolioRepository goalPortfolioRepository,
    IInstrumentRepository instrumentRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AcceptPortfolioProposalCommand, PortfolioProposalResponse>
{
    private const string Market = "us"; // second instance per D5 when EGX activates

    public async Task<Result<PortfolioProposalResponse>> Handle(
        AcceptPortfolioProposalCommand request, CancellationToken cancellationToken)
    {
        PortfolioProposal? proposal = await proposalRepository.GetByIdAsync(request.ProposalId, cancellationToken);
        if (proposal is null)
        {
            return Result.Fail(ProposalNotFound(request.ProposalId));
        }

        // Ownership runs through the goal — a proposal has no user of its own.
        Goal? goal = await goalRepository.GetByIdAsync(proposal.GoalId, cancellationToken);
        if (goal is null || goal.UserId != request.UserId)
        {
            return Result.Fail(UnauthorizedAccess);
        }

        if (proposal.Status == ProposalStatus.Superseded)
        {
            return Result.Fail(AlreadySuperseded);
        }

        // Re-accepting the already-accepted proposal is a no-op: leave the live
        // portfolio untouched rather than restarting its high-water mark.
        if (!proposal.Accept())
        {
            return Result.Ok(PortfolioProposalResponse.From(proposal));
        }

        // Supersede whatever was accepted before and close its live portfolio.
        IReadOnlyList<PortfolioProposal> priorAccepted =
            await proposalRepository.GetAcceptedByGoalIdAsync(proposal.GoalId, cancellationToken);
        foreach (PortfolioProposal prior in priorAccepted.Where(p => p.Id != proposal.Id))
        {
            prior.Supersede();
        }

        GoalPortfolio? existing = await goalPortfolioRepository.GetActiveByGoalIdAsync(proposal.GoalId, cancellationToken);
        existing?.Close();

        // Turn the frozen proposal into a live, valued portfolio using today's
        // prices as entry prices (the registry's nightly closes).
        IReadOnlyList<Instrument> instruments = await instrumentRepository.GetActiveByMarketAsync(Market, cancellationToken);
        var priceBySymbol = instruments
            .Where(i => i.LastClose is > 0)
            .ToDictionary(i => i.Symbol, i => i.LastClose!.Value, StringComparer.OrdinalIgnoreCase);

        List<(string, string, double, double)> entries = proposal.GetPositions()
            .Select(p => (p.Symbol, p.Sleeve, p.Weight, priceBySymbol.GetValueOrDefault(p.Symbol)))
            .ToList();

        GoalPortfolio portfolio = GoalPortfolio.Open(
            proposal.GoalId,
            goal.UserId,
            proposal.Id,
            proposal.Amount,
            proposal.DrawdownAlertPct,
            entries);
        await goalPortfolioRepository.AddAsync(portfolio, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(PortfolioProposalResponse.From(proposal));
    }
}
