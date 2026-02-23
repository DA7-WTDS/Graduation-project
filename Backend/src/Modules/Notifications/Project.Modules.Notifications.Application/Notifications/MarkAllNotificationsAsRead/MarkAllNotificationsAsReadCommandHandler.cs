using System.Collections.Generic;
using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Notifications.Application.Abstractions.Data;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Application.Notifications.MarkAllNotificationsAsRead;

internal sealed class MarkAllNotificationsAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Notification> unread = await notificationRepository.GetUnreadByUserIdAsync(
            request.UserId,
            cancellationToken);

        foreach (Notification notification in unread)
        {
            notification.MarkAsRead();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
