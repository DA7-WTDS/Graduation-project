using FluentAssertions;
using NSubstitute;
using Project.Modules.Notifications.Application.Abstractions.Data;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Application.Notifications.MarkNotificationAsRead;
using Project.Modules.Notifications.Domain.Notifications;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Notifications.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MarkNotificationAsReadCommandHandler _handler;

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _handler = new MarkNotificationAsReadCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenNotificationNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Notification?)null);

        var result = await _handler.Handle(new MarkNotificationAsReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenNotificationBelongsToAnotherUser()
    {
        var notification = Notification.Create(Guid.NewGuid(), "T", "M");
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(notification);

        // command UserId differs from the notification owner
        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        notification.IsRead.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_MarkRead_AndSave_WhenOwnedByUser()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(userId, "T", "M");
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id, userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
