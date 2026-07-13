using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Proposals;
using static Project.Modules.Portfolio.Domain.Proposals.ProposalErrors;

namespace Project.Modules.Portfolio.Application.Proposals.AcceptProposal;

internal sealed class AcceptPortfolioProposalCommandHandler(
    IPortfolioProposalRepository proposalRepository,
    IGoalRepository goalRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AcceptPortfolioProposalCommand, PortfolioProposalResponse>
{
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

        // Supersede whatever was accepted before (skip the target itself, so
        // re-accepting the current proposal stays a no-op rather than self-superseding).
        IReadOnlyList<PortfolioProposal> priorAccepted =
            await proposalRepository.GetAcceptedByGoalIdAsync(proposal.GoalId, cancellationToken);
        foreach (PortfolioProposal prior in priorAccepted.Where(p => p.Id != proposal.Id))
        {
            prior.Supersede();
        }

        proposal.Accept();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(PortfolioProposalResponse.From(proposal));
    }
}
