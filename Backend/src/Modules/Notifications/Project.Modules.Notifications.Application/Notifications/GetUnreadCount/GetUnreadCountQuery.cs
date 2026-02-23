using Project.Common.Application.Messaging;

namespace Project.Modules.Notifications.Application.Notifications.GetUnreadCount;

public sealed record GetUnreadCountQuery(Guid UserId) : IQuery<int>;
