using System.Collections.Generic;
using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Notifications.Application.Notifications;
using Project.Modules.Notifications.Application.Notifications.GetNotifications;

namespace Project.Modules.Notifications.Presentation.Notifications;

internal sealed class GetNotifications : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", async (
            ISender sender,
            ClaimsPrincipal claimsPrincipal,
            int page = 1,
            int pageSize = 20) =>
        {
            Result<IReadOnlyList<NotificationResponse>> result = await sender.Send(
                new GetNotificationsQuery(claimsPrincipal.GetUserId(), page, pageSize));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Notifications)
        .WithName("GetNotifications")
        .WithSummary("Get user notifications")
        .WithDescription("Retrieves a paginated list of notifications for the authenticated user")
        .Produces<IReadOnlyList<NotificationResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
