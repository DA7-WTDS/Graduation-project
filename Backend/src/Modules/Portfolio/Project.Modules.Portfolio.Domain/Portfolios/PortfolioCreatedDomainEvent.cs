using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Portfolios;

public sealed record PortfolioCreatedDomainEvent(Guid Id,
    DateTime OccuredOnUtc,
    Guid PortfolioId,
    Guid UserId) : DomainEvent(Id, OccuredOnUtc);
