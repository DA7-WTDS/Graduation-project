using Project.Modules.Notifications.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Project.Common.Infrastructure.Inbox;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Notifications.Infrastructure.Notifications;

namespace Project.Modules.Notifications.Infrastructure.Database;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Notifications);

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
    }
}
