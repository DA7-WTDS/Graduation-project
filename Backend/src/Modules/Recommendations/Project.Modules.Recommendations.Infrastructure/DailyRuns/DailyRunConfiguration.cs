using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Infrastructure.DailyRuns;

internal sealed class DailyRunConfiguration : IEntityTypeConfiguration<DailyRun>
{
    public void Configure(EntityTypeBuilder<DailyRun> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.GeneratedAt).IsRequired();
        builder.Property(r => r.Count).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        // § 6.2 kill-switch lifecycle. Stored as text for pg_admin readability.
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(r => r.StatusReason).HasMaxLength(2000);
        builder.Property(r => r.StatusChangedAt).IsRequired();

        builder.HasIndex(r => r.GeneratedAt);
        // Serves the hot query: latest run WHERE status = 'Published'.
        builder.HasIndex(r => new { r.Status, r.GeneratedAt });

        builder.HasMany(r => r.Predictions)
            .WithOne()
            .HasForeignKey(p => p.DailyRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(DailyRun.Predictions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
