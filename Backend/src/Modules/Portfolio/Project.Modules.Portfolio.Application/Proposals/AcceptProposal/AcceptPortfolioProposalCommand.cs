using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Proposals.AcceptProposal;

/// <summary>Accepts a proposal as the goal's current target, superseding any
/// previously accepted proposal for that goal.</summary>
public sealed record AcceptPortfolioProposalCommand(Guid UserId, Guid ProposalId)
    : ICommand<PortfolioProposalResponse>;
