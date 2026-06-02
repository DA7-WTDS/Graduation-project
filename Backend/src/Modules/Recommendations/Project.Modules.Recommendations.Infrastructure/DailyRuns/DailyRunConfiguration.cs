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

        builder.HasIndex(r => r.GeneratedAt);

        builder.HasMany(r => r.Predictions)
            .WithOne()
            .HasForeignKey(p => p.DailyRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(DailyRun.Predictions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
