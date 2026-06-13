using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Notifications.Application.Notifications.MarkNotificationAsRead;

namespace Project.Modules.Notifications.Presentation.Notifications;

internal sealed class MarkAsRead : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/notifications/{id}/read", async (
            Guid id,
            ISender sender,
            ClaimsPrincipal claimsPrincipal) =>
        {
            Result result = await sender.Send(new MarkNotificationAsReadCommand(id, claimsPrincipal.GetUserId()));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Notifications)
        .WithName("MarkNotificationAsRead")
        .WithSummary("Mark notification as read")
        .WithDescription("Marks a specific notification as read for the authenticated user")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
