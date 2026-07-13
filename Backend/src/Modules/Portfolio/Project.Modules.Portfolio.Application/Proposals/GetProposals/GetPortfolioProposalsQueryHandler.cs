using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Proposals;
using static Project.Modules.Portfolio.Domain.Goals.GoalErrors;

namespace Project.Modules.Portfolio.Application.Proposals.GetProposals;

internal sealed class GetPortfolioProposalsQueryHandler(
    IGoalRepository goalRepository,
    IPortfolioProposalRepository proposalRepository)
    : IQueryHandler<GetPortfolioProposalsQuery, IReadOnlyList<PortfolioProposalResponse>>
{
    public async Task<Result<IReadOnlyList<PortfolioProposalResponse>>> Handle(
        GetPortfolioProposalsQuery request, CancellationToken cancellationToken)
    {
        Goal? goal = await goalRepository.GetByIdAsync(request.GoalId, cancellationToken);
        if (goal is null)
        {
            return Result.Fail(GoalNotFound(request.GoalId));
        }

        if (goal.UserId != request.UserId)
        {
            return Result.Fail(UnauthorizedAccess);
        }

        IReadOnlyList<PortfolioProposal> proposals =
            await proposalRepository.GetByGoalIdAsync(request.GoalId, cancellationToken);

        return Result.Ok<IReadOnlyList<PortfolioProposalResponse>>(
            proposals.Select(PortfolioProposalResponse.From).ToList());
    }
}
