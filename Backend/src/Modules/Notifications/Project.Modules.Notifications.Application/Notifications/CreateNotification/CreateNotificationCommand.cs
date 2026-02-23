using Project.Common.Application.Messaging;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Application.Notifications.CreateNotification;

public sealed record CreateNotificationCommand(
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type = NotificationType.Info) : ICommand<Guid>;
