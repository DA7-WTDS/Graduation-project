using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Modules.Users.Application.Users.GetUser;
using Project.Modules.Users.Domain.Users;
using Project.Modules.Users.Infrastructure.Database;
using Project.Modules.Users.PublicApi;
using UserResponse = Project.Modules.Users.PublicApi.UserResponse;

namespace Project.Modules.Users.Infrastructure.PublicApi;

public class UsersApi(ISender sender, UsersDbContext dbContext) : IUsersApi
{
    public async Task<UserResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Result<Application.Users.GetUser.UserResponse> result = await sender.Send(new GetUserQuery(userId), cancellationToken);

        return result.IsSuccess
            ? new UserResponse(result.Value.Id, result.Value.Email, result.Value.FirstName, result.Value.LastName)
            : null;
    }

    public async Task<IReadOnlyList<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == Role.Admin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }
}
