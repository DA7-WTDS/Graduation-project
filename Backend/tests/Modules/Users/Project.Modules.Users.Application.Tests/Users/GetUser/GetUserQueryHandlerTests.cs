using FluentAssertions;
using NSubstitute;
using Project.Common.Application.Caching;
using Project.Common.Application.Data;
using Project.Modules.Users.Application.Users.GetUser;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Project.Modules.Users.Domain.Users;

namespace Project.Modules.Users.Application.Tests.Users.GetUser;

public class GetUserQueryHandlerTests
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICacheService _cacheService;
    private readonly GetUserQueryHandler _handler;

    public GetUserQueryHandlerTests()
    {
        _connectionFactory = Substitute.For<IDbConnectionFactory>();
        _cacheService = Substitute.For<ICacheService>();

        _handler = new GetUserQueryHandler(_connectionFactory, _cacheService);
    }

    [Fact]
    public async Task Handle_Should_ReturnUserFromCache_WhenCacheHit()
    {
        // Arrange
        var query = new GetUserQuery(Guid.NewGuid());
        var cachedUser = new UserResponse(
            query.Id, 
            "John", 
            "Doe", 
            "test@example.com", 
            "User",
            DateTime.UtcNow
        );

        // For GetOrCreateAsync, if we mock it to return the cachedUser, it won't invoke the factory delegate
        _cacheService.GetOrCreateAsync(
            query.ToString(),
            Arg.Any<Func<CancellationToken, Task<UserResponse>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cachedUser));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedUser);
        
        // We ensure that connection factory was not called
        await _connectionFactory.DidNotReceive().OpenConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var query = new GetUserQuery(Guid.NewGuid());

        _cacheService.GetOrCreateAsync(
            query.ToString(),
            Arg.Any<Func<CancellationToken, Task<UserResponse>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UserResponse?>(null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == UserErrors.UserNotFound(query.Id).Message);
    }
}
