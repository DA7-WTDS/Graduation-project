using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Project.Common.Application.EventBus;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.IntegrationEvents;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.DailyRuns.Ingest;

public class DailyRunIngestedDomainEventHandlerTests
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<DailyRunIngestedDomainEventHandler> _logger;
    private readonly DailyRunIngestedDomainEventHandler _handler;

    public DailyRunIngestedDomainEventHandlerTests()
    {
        _eventBus = Substitute.For<IEventBus>();
        _logger = Substitute.For<ILogger<DailyRunIngestedDomainEventHandler>>();

        _handler = new DailyRunIngestedDomainEventHandler(_eventBus, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_PublishIntegrationEvent()
    {
        // Arrange
        var domainEvent = new DailyRunIngestedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow, "Quarantined", "coverage too low");

        // Act
        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<DailyRunIngestedIntegrationEvent>(e =>
                e.Id == domainEvent.Id &&
                e.DailyRunId == domainEvent.DailyRunId &&
                e.GeneratedAt == domainEvent.GeneratedAt &&
                e.Status == "Quarantined" &&
                e.StatusReason == "coverage too low"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_WhenPublishFails()
    {
        // Arrange
        var domainEvent = new DailyRunIngestedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow, "Published", null);
        var expectedException = new Exception("EventBus error");

        _eventBus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act & Assert
        var action = async () => await _handler.HandleAsync(domainEvent, CancellationToken.None);
        await action.Should().ThrowAsync<Exception>().WithMessage("EventBus error");
    }
}
