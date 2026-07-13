using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Application.Allocation;
using Project.Modules.Portfolio.Domain.Proposals;

namespace Project.Modules.Portfolio.Application.Proposals.CreateProposal;

internal sealed class CreatePortfolioProposalCommandHandler(
    PortfolioProposalBuilder builder,
    IPortfolioProposalRepository proposalRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePortfolioProposalCommand, PortfolioProposalResponse>
{
    public async Task<Result<PortfolioProposalResponse>> Handle(
        CreatePortfolioProposalCommand request, CancellationToken cancellationToken)
    {
        Result<BuiltAllocation> built = await builder.BuildAsync(request.UserId, request.GoalId, cancellationToken);
        if (built.IsFailed)
        {
            return Result.Fail(built.Errors);
        }

        BuiltAllocation b = built.Value;

        int nextVersion = await proposalRepository.GetLatestVersionAsync(b.Goal.Id, cancellationToken) + 1;

        var proposal = PortfolioProposal.Create(
            b.Goal.Id,
            nextVersion,
            b.Template.Key,
            b.Template.Name,
            b.Template.RebalanceCadence,
            b.Template.DrawdownAlertPct,
            b.Profile.RiskBand,
            b.Profile.EffectiveRisk,
            b.Amount,
            b.Allocation.Positions,
            b.Allocation.Assumptions,
            b.Allocation.InputsHash);

        await proposalRepository.AddAsync(proposal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(PortfolioProposalResponse.From(proposal));
    }
}
