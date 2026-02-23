using Project.Common.Application.Messaging;

namespace Project.Modules.Notifications.Application.Notifications.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId, Guid UserId) : ICommand;
