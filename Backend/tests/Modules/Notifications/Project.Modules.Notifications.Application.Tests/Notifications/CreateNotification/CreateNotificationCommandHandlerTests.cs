using FluentAssertions;
using NSubstitute;
using Project.Modules.Notifications.Application.Abstractions.Data;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Notifications.CreateNotification;

public class CreateNotificationCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerTests()
    {
        _handler = new CreateNotificationCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_AddNotificationAndSave()
    {
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Message", NotificationType.Success);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _repository.Received(1).Add(Arg.Is<Notification>(n =>
            n.UserId == command.UserId &&
            n.Title == "Title" &&
            n.Message == "Message" &&
            n.Type == NotificationType.Success &&
            n.IsRead == false));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
