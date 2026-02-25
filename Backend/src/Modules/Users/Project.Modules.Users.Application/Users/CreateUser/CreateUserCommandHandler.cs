using FluentResults;
using Microsoft.Extensions.Logging;
using Project.Common.Application.Messaging;
using Project.Modules.Users.Application.Abstractions.Data;
using Project.Modules.Users.Application.Abstractions.Security;
using Project.Modules.Users.Application.Abstractions.Users;
using Project.Modules.Users.Domain.Users;

namespace Project.Modules.Users.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<CreateUserCommandHandler> logger)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting user registration for email: {Email}", request.Email);

            User? existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (existingUser is not null)
            {
                logger.LogWarning("User registration failed - email already exists: {Email}", request.Email);
                return Result.Fail(UserErrors.UserAlreadyExists);
            }

            string hashedPassword = passwordHasher.HashPassword(request.Password);

            var user = User.Create(
                request.FirstName,
                request.LastName,
                request.Email,
                hashedPassword
            );

            logger.LogInformation("Adding user {UserId} to repository", user.Id);
            await userRepository.AddAsync(user, cancellationToken);

            logger.LogInformation("Saving changes for user {UserId}", user.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} registered successfully", user.Id);
            return Result.Ok(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Critical error during user registration.\n" +
                "Email: {Email}\n" +
                "FirstName: {FirstName}\n" +
                "LastName: {LastName}\n" +
                "Exception Type: {ExceptionType}\n" +
                "Full Exception: {FullException}",
                request.Email,
                request.FirstName,
                request.LastName,
                ex.GetType().Name,
                ex.ToString());
            throw;
        }
    }
}
