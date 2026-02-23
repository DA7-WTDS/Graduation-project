using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Emails.SendWelcomeEmail;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Users.IntegrationEvents.Users;

namespace Project.Modules.Notifications.Presentation.Users;

public sealed class UserCreatedIntegrationEventHandler(
    ISender sender,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : IntegrationEventHandler<UserCreatedIntegrationEvent>
{
    public async override Task Handle(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        // 1. Send Welcome Email (Best effort)
        try
        {
            Result emailResult = await sender.Send(new SendWelcomeEmailCommand(
                integrationEvent.Email,
                integrationEvent.FirstName,
                integrationEvent.LastName), cancellationToken);

            if (emailResult.IsFailed)
            {
                logger.LogWarning("Failed to send welcome email for user {UserId}: {Error}", 
                    integrationEvent.UserId, emailResult.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while sending welcome email for user {UserId}", integrationEvent.UserId);
        }

        // 2. Create In-App Notification
        try
        {
            Result<Guid> notificationResult = await sender.Send(new CreateNotificationCommand(
                integrationEvent.UserId,
                "Welcome to SmartInvest AI!",
                $"Hi {integrationEvent.FirstName}, we're excited to have you on board! Start by completing your profile and setting up your first portfolio.",
                NotificationType.Success), cancellationToken);

            if (notificationResult.IsFailed)
            {
                logger.LogError("Failed to create welcome notification for user {UserId}: {Error}", 
                    integrationEvent.UserId, notificationResult.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while creating welcome notification for user {UserId}", integrationEvent.UserId);
        }
    }
}
