using FluentAssertions;
using NSubstitute;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Application.Notifications.GetUnreadCount;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Notifications.GetUnreadCount;

public class GetUnreadCountQueryHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly GetUnreadCountQueryHandler _handler;

    public GetUnreadCountQueryHandlerTests()
    {
        _handler = new GetUnreadCountQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnreadCount()
    {
        var userId = Guid.NewGuid();
        _repository.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>()).Returns(7);

        var result = await _handler.Handle(new GetUnreadCountQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7);
    }
}
