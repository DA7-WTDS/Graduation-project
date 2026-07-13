using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Proposals.GetProposals;

/// <summary>All proposals for a goal, newest version first.</summary>
public sealed record GetPortfolioProposalsQuery(Guid UserId, Guid GoalId)
    : IQuery<IReadOnlyList<PortfolioProposalResponse>>;
