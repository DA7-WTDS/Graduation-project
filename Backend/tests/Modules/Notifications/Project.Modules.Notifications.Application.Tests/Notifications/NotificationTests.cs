using FluentAssertions;
using Project.Modules.Notifications.Domain.Notifications;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Notifications;

public class NotificationTests
{
    [Fact]
    public void Create_Should_SetFieldsAndStartUnread()
    {
        var userId = Guid.NewGuid();

        var notification = Notification.Create(userId, "Title", "Message", NotificationType.Success);

        notification.Id.Should().NotBeEmpty();
        notification.UserId.Should().Be(userId);
        notification.Title.Should().Be("Title");
        notification.Message.Should().Be("Message");
        notification.Type.Should().Be(NotificationType.Success);
        notification.IsRead.Should().BeFalse();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Create_Should_DefaultTypeToInfo()
    {
        var notification = Notification.Create(Guid.NewGuid(), "T", "M");

        notification.Type.Should().Be(NotificationType.Info);
    }

    [Fact]
    public void MarkAsRead_Should_SetIsReadTrue()
    {
        var notification = Notification.Create(Guid.NewGuid(), "T", "M");

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
    }
}
