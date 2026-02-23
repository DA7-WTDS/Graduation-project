using Project.Common.Application.Messaging;

namespace Project.Modules.Notifications.Application.Notifications.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand(Guid UserId) : ICommand;
