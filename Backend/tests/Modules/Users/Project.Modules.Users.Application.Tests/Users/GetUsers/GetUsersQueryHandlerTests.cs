using FluentAssertions;
using NSubstitute;
using Project.Common.Application.Caching;
using Project.Common.Application.Data;
using Project.Modules.Users.Application.Users.GetUsers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Users.Application.Tests.Users.GetUsers;

public class GetUsersQueryHandlerTests
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICacheService _cacheService;
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _connectionFactory = Substitute.For<IDbConnectionFactory>();
        _cacheService = Substitute.For<ICacheService>();

        _handler = new GetUsersQueryHandler(_connectionFactory, _cacheService);
    }

    [Fact]
    public async Task Handle_Should_ReturnUsersFromCache_WhenCacheHit()
    {
        // Arrange
        var query = new GetUsersQuery();
        var cachedUsers = new List<UserResponse> 
        { 
            new UserResponse(Guid.NewGuid(), "User", "One", "test1@example.com"),
            new UserResponse(Guid.NewGuid(), "User", "Two", "test2@example.com")
        };

        _cacheService.GetAsync<IReadOnlyCollection<UserResponse>>(
            query.ToString(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<UserResponse>>(cachedUsers));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedUsers);

        // Verify we didn't try to query the DB
        await _connectionFactory.DidNotReceive().OpenConnectionAsync(Arg.Any<CancellationToken>());
    }
}
