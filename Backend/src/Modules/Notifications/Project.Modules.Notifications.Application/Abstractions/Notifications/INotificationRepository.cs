using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Application.Abstractions.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(Notification notification);
}
