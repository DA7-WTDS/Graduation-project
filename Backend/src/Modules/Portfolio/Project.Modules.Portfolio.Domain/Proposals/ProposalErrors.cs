using FluentResults;
using Project.Common.Domain;
using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Proposals;

public static class ProposalErrors
{
    public static Error ProposalNotFound(Guid proposalId) =>
        new Error($"Portfolio proposal with ID {proposalId} not found.")
            .WithErrorType(ErrorType.NotFound);

    public static Error UnauthorizedAccess =>
        new Error("You are not authorized to access this proposal.")
            .WithErrorType(ErrorType.Forbidden);

    public static Error AlreadySuperseded =>
        new Error("This proposal has been superseded and can no longer be accepted. Generate a fresh one.")
            .WithErrorType(ErrorType.Conflict);
}
