using Project.Common.Domain.Abstractions;

namespace Project.Modules.Notifications.Domain.Notifications;

public sealed class Notification : Entity
{
    private Notification() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Notification Create(
        Guid userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
