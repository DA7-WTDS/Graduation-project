using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Project.Modules.Notifications.Application.Abstractions.Data;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Application.Notifications.MarkAllNotificationsAsRead;
using Project.Modules.Notifications.Domain.Notifications;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Notifications.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MarkAllNotificationsAsReadCommandHandler _handler;

    public MarkAllNotificationsAsReadCommandHandlerTests()
    {
        _handler = new MarkAllNotificationsAsReadCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_MarkAllUnreadAsRead_AndSave()
    {
        var userId = Guid.NewGuid();
        var a = Notification.Create(userId, "A", "A");
        var b = Notification.Create(userId, "B", "B");
        _repository.GetUnreadByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Notification> { a, b });

        var result = await _handler.Handle(new MarkAllNotificationsAsReadCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        a.IsRead.Should().BeTrue();
        b.IsRead.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Succeed_WhenNothingUnread()
    {
        _repository.GetUnreadByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());

        var result = await _handler.Handle(new MarkAllNotificationsAsReadCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
