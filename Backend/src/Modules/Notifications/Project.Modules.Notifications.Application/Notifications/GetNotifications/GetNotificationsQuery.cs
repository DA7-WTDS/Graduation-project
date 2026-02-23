using System.Collections.Generic;
using Project.Common.Application.Messaging;

namespace Project.Modules.Notifications.Application.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IQuery<IReadOnlyList<NotificationResponse>>;
