using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Common.Application.Messaging;
using Project.Modules.Users.Domain.Users;
using Project.Modules.Users.IntegrationEvents.Users;

namespace Project.Modules.Users.Application.Users.CreateUser;

internal sealed class UserCreatedDomainEventHandler(
    IEventBus eventBus,
    ILogger<UserCreatedDomainEventHandler> logger)
    : DomainEventHandler<UserCreatedDomainEvent>
{
    public override async Task HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Handling UserCreatedDomainEvent for user {UserId}", domainEvent.UserId);

            logger.LogInformation("Publishing UserCreatedIntegrationEvent for user {UserId}", domainEvent.UserId);
            await eventBus.PublishAsync(new UserCreatedIntegrationEvent(
                    domainEvent.Id,
                    domainEvent.OccurredOnUtc,
                    domainEvent.UserId,
                    domainEvent.Email,
                    domainEvent.FirstName,
                    domainEvent.LastName,
                    domainEvent.Role), cancellationToken);

            logger.LogInformation("UserCreatedIntegrationEvent published successfully for user {UserId}", domainEvent.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Critical error in UserCreatedDomainEventHandler.\n" +
                "UserId: {UserId}\n" +
                "EventId: {EventId}\n" +
                "Exception Type: {ExceptionType}\n" +
                "Full Exception: {FullException}",
                domainEvent.UserId,
                domainEvent.Id,
                ex.GetType().Name,
                ex.ToString());
            throw;
        }
    }
}
