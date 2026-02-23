using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Notifications.Application.Notifications.GetUnreadCount;

namespace Project.Modules.Notifications.Presentation.Notifications;

internal sealed class GetUnreadCount : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/notifications/unread-count", async (
            ISender sender,
            ClaimsPrincipal claimsPrincipal) =>
        {
            Result<int> result = await sender.Send(new GetUnreadCountQuery(claimsPrincipal.GetUserId()));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Notifications)
        .WithName("GetUnreadCount")
        .WithSummary("Get unread notifications count")
        .WithDescription("Retrieves the count of unread notifications for the authenticated user")
        .Produces<int>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
