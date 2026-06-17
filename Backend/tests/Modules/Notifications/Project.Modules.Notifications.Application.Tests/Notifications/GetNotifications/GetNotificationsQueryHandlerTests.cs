using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Application.Notifications.GetNotifications;
using Project.Modules.Notifications.Domain.Notifications;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Notifications.GetNotifications;

public class GetNotificationsQueryHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly GetNotificationsQueryHandler _handler;

    public GetNotificationsQueryHandlerTests()
    {
        _handler = new GetNotificationsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Should_MapNotificationsToResponses()
    {
        var userId = Guid.NewGuid();
        var query = new GetNotificationsQuery(userId, 1, 20);
        var notification = Notification.Create(userId, "Title", "Message", NotificationType.Warning);

        _repository.GetByUserIdAsync(userId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Notification> { notification });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value[0];
        dto.Id.Should().Be(notification.Id);
        dto.UserId.Should().Be(userId);
        dto.Title.Should().Be("Title");
        dto.Type.Should().Be("Warning");
        dto.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_WhenNoNotifications()
    {
        var query = new GetNotificationsQuery(Guid.NewGuid());

        _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
