using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Notifications.Application.Abstractions.Notifications;

namespace Project.Modules.Notifications.Application.Notifications.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler(
    INotificationRepository notificationRepository)
    : IQueryHandler<GetUnreadCountQuery, int>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        int count = await notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
        return Result.Ok(count);
    }
}
