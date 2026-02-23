using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Notifications.Application.Abstractions.Data;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Application.Notifications.CreateNotification;

internal sealed class CreateNotificationCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateNotificationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            request.UserId,
            request.Title,
            request.Message,
            request.Type);

        notificationRepository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(notification.Id);
    }
}
