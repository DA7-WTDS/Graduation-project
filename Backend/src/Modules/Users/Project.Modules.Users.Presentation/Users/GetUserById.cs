using System.Security.Claims;
using FluentResults;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Users.Application.Users.GetUser;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Project.Modules.Users.Presentation.Users;

internal sealed class GetUserById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id}", async (ISender sender, ClaimsPrincipal claimsPrincipal, Guid id) =>
        {
            // A user may only fetch their own record (prevents enumerating others by id).
            if (claimsPrincipal.GetUserId() != id)
            {
                return Results.Problem(title: "Forbidden", statusCode: StatusCodes.Status403Forbidden);
            }

            Result<UserResponse> result = await sender.Send(new GetUserQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithName(nameof(GetUserById))
        .WithTags(Tags.Users)
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves a specific user by their unique identifier (the authenticated user only)")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}