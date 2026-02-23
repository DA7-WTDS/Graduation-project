using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Notifications.Application.Abstractions.Data;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Application.Notifications.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        Notification? notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null || notification.UserId != request.UserId)
            return Result.Fail($"Notification {request.NotificationId} not found.");

        notification.MarkAsRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
