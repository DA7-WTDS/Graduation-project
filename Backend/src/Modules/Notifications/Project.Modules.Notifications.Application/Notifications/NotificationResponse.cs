namespace Project.Modules.Notifications.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt);
