using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Proposals.CreateProposal;

/// <summary>Runs the optimizer for the goal and persists the result as the next
/// immutable proposal version.</summary>
public sealed record CreatePortfolioProposalCommand(Guid UserId, Guid GoalId)
    : ICommand<PortfolioProposalResponse>;
