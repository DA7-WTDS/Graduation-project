using Project.Common.Application.EventBus;

namespace Project.Modules.Portfolio.IntegrationEvents;

/// <summary>
/// The nightly shadow-portfolio run executed but wrote no snapshots for a market
/// that has model portfolios — the track record didn't advance (stale prices, no
/// published daily run, or an incomplete price book). An ops signal, not a user
/// concern: silence should never look like success.
/// </summary>
public sealed record ShadowRunBlockedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string Market,
    int PortfolioCount,
    string Reason
) : IntegrationEvent(Id, OccurredOnUtc);
