using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project.Modules.Notifications.Application.Abstractions.Notifications;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Notifications.Infrastructure.Database;

namespace Project.Modules.Notifications.Infrastructure.Notifications;

internal sealed class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
    }

    public void Add(Notification notification)
    {
        dbContext.Notifications.Add(notification);
    }
}
