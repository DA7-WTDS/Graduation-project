using System.Collections.Generic;
using System.Linq;
using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Application.Notifications.GetNotifications;

internal sealed class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository)
    : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<Result<IReadOnlyList<NotificationResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Notification> notifications = await notificationRepository.GetByUserIdAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        IReadOnlyList<NotificationResponse> response = notifications
            .Select(n => new NotificationResponse(
                n.Id,
                n.UserId,
                n.Title,
                n.Message,
                n.Type.ToString(),
                n.IsRead,
                n.CreatedAt))
            .ToList();

        return Result.Ok(response);
    }
}
