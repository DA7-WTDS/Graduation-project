using Microsoft.Extensions.Logging;
using NSubstitute;
using Project.Common.Application.EventBus;
using Project.Modules.Users.Application.Users.CreateUser;
using Project.Modules.Users.Domain.Users;
using Project.Modules.Users.IntegrationEvents.Users;
using Xunit;

namespace Project.Modules.Users.Application.Tests.Users.CreateUser;

public class UserCreatedDomainEventHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ILogger<UserCreatedDomainEventHandler> _logger = Substitute.For<ILogger<UserCreatedDomainEventHandler>>();
    private readonly UserCreatedDomainEventHandler _handler;

    public UserCreatedDomainEventHandlerTests()
    {
        _handler = new UserCreatedDomainEventHandler(_eventBus, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_PublishUserCreatedIntegrationEvent()
    {
        var userId = Guid.NewGuid();
        var domainEvent = new UserCreatedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow, userId, "john@doe.com", "John", "Doe", "User");

        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserCreatedIntegrationEvent>(e =>
                e.UserId == userId &&
                e.Email == "john@doe.com" &&
                e.FirstName == "John" &&
                e.LastName == "Doe" &&
                e.Role == "User"),
            Arg.Any<CancellationToken>());
    }
}
