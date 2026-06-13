using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Project.Modules.Users.Application.Abstractions.Data;
using Project.Modules.Users.Application.Abstractions.Security;
using Project.Modules.Users.Application.Abstractions.Users;
using Project.Modules.Users.Application.Users.CreateUser;
using Project.Modules.Users.Domain.Users;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Users.Application.Tests.Users.CreateUser;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _logger = Substitute.For<ILogger<CreateUserCommandHandler>>();

        _handler = new CreateUserCommandHandler(
            _userRepository,
            _unitOfWork,
            _passwordHasher,
            _logger);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new CreateUserCommand("John", "Doe", "test@example.com", "password");
        
        var existingUser = User.Create("Jane", "Doe", command.Email, "hash");
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == UserErrors.UserAlreadyExists.Message);

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessAndSaveUser_WhenEmailIsUnique()
    {
        // Arrange
        var command = new CreateUserCommand("John", "Doe", "test@example.com", "password");
        var hashedPassword = "hashed_password";

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User)null);

        _passwordHasher.HashPassword(command.Password).Returns(hashedPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => 
                u.Email == command.Email && 
                u.FirstName == command.FirstName && 
                u.LastName == command.LastName && 
                u.HashedPassword == hashedPassword), 
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
