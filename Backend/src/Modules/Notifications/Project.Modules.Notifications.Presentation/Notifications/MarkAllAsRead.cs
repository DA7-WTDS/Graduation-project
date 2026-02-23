using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Notifications.Application.Notifications.MarkAllNotificationsAsRead;

namespace Project.Modules.Notifications.Presentation.Notifications;

internal sealed class MarkAllAsRead : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/notifications/read-all", async (
            ISender sender,
            ClaimsPrincipal claimsPrincipal) =>
        {
            Result result = await sender.Send(new MarkAllNotificationsAsReadCommand(claimsPrincipal.GetUserId()));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Notifications)
        .WithName("MarkAllNotificationsAsRead")
        .WithSummary("Mark all notifications as read")
        .WithDescription("Marks all unread notifications as read for the authenticated user")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
