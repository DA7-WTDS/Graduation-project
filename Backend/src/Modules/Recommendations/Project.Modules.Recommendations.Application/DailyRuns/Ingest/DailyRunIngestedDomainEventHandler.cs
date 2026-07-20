using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.IntegrationEvents;

namespace Project.Modules.Recommendations.Application.DailyRuns.Ingest;

internal sealed class DailyRunIngestedDomainEventHandler(
    IEventBus eventBus,
    ILogger<DailyRunIngestedDomainEventHandler> logger)
    : DomainEventHandler<DailyRunIngestedDomainEvent>
{
    public override async Task HandleAsync(DailyRunIngestedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Handling DailyRunIngestedDomainEvent for run {DailyRunId}", domainEvent.DailyRunId);

            logger.LogInformation("Publishing DailyRunIngestedIntegrationEvent for run {DailyRunId}", domainEvent.DailyRunId);
            await eventBus.PublishAsync(new DailyRunIngestedIntegrationEvent(
                domainEvent.Id,
                domainEvent.OccurredOnUtc,
                domainEvent.DailyRunId,
                domainEvent.GeneratedAt,
                domainEvent.Status,
                domainEvent.StatusReason), cancellationToken);

            logger.LogInformation("DailyRunIngestedIntegrationEvent published successfully for run {DailyRunId}", domainEvent.DailyRunId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Critical error in DailyRunIngestedDomainEventHandler.\n" +
                "DailyRunId: {DailyRunId}\n" +
                "EventId: {EventId}\n" +
                "Exception Type: {ExceptionType}\n" +
                "Full Exception: {FullException}",
                domainEvent.DailyRunId,
                domainEvent.Id,
                ex.GetType().Name,
                ex.ToString());
            throw;
        }
    }
}
