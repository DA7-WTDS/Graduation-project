using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Notifications.Domain.Notifications;

namespace Project.Modules.Notifications.Infrastructure.Notifications;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Index to efficiently fetch notifications by user and read status
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
    }
}
