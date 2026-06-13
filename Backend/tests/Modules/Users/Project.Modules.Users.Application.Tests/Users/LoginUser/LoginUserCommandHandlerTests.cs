using FluentAssertions;
using NSubstitute;
using Project.Modules.Users.Application.Abstractions.Security;
using Project.Modules.Users.Application.Abstractions.Users;
using Project.Modules.Users.Application.Users.LoginUser;
using Project.Modules.Users.Domain.Users;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Users.Application.Tests.Users.LoginUser;

public class LoginUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        _handler = new LoginUserCommandHandler(_userRepository, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var command = new LoginUserCommand("test@example.com", "password");
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == UserErrors.CredentialsNotCorrect.Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPasswordIsIncorrect()
    {
        // Arrange
        var command = new LoginUserCommand("test@example.com", "wrong_password");
        var user = User.Create("John", "Doe", command.Email, "hashed_pass");
        
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        
        _passwordHasher.VerifyHashedPassword(user.HashedPassword, command.Password)
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == UserErrors.CredentialsNotCorrect.Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithToken_WhenCredentialsAreCorrect()
    {
        // Arrange
        var command = new LoginUserCommand("test@example.com", "password");
        var user = User.Create("John", "Doe", command.Email, "hashed_pass");
        var token = "jwt_token";

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        
        _passwordHasher.VerifyHashedPassword(user.HashedPassword, command.Password)
            .Returns(true);

        _jwtTokenGenerator.GenerateToken(user.Id, user.Role.ToString())
            .Returns(token);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be(token);
    }
}
