using System.Globalization;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Presentation.Ops;
using Project.Modules.Portfolio.IntegrationEvents;

namespace Project.Modules.Notifications.Presentation.Portfolios;

/// <summary>
/// Ops alert: the nightly shadow-portfolio run produced no snapshots, so the public
/// model-portfolio track record did not advance. A missed night must be visible, not
/// silent — gaps in a published track record look worse than a short one.
/// </summary>
public sealed class ShadowRunBlockedIntegrationEventHandler(IOpsAlert opsAlert)
    : IntegrationEventHandler<ShadowRunBlockedIntegrationEvent>
{
    public override Task Handle(
        ShadowRunBlockedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        string day = integrationEvent.OccurredOnUtc.ToString("MMMM d", CultureInfo.InvariantCulture);
        string message =
            $"The {integrationEvent.Market.ToUpperInvariant()} model-portfolio track record did not " +
            $"advance on {day}: {integrationEvent.Reason}. {integrationEvent.PortfolioCount} portfolio(s) " +
            $"affected. Check the pipeline run and instrument prices, then re-run via " +
            $"/api/internal/shadow/run.";

        return opsAlert.RaiseAsync("Shadow track record did not update", message, cancellationToken);
    }
}
