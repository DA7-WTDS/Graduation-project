using System.Security.Claims;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Infrastructure.Authentication;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Presentation.Notifications;

internal sealed class CreateTestNotification : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notifications/test", async (
            ISender sender,
            ClaimsPrincipal claimsPrincipal) =>
        {
            var result = await sender.Send(new CreateNotificationCommand(
                claimsPrincipal.GetUserId(),
                "System Test",
                "This is a test notification to verify the system is working.",
                NotificationType.Info));

            return result.Match(_ => Results.Ok("Test notification created"), ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Notifications);
    }
}
