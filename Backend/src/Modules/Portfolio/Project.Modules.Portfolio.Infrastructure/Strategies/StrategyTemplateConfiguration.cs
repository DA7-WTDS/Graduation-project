using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Portfolio.Domain.Strategies;

namespace Project.Modules.Portfolio.Infrastructure.Strategies;

internal sealed class StrategyTemplateConfiguration : IEntityTypeConfiguration<StrategyTemplate>
{
    public void Configure(EntityTypeBuilder<StrategyTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Key)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Key).IsUnique();

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Postgres text[] — GoalType enum names.
        builder.Property(t => t.GoalTypes)
            .IsRequired();

        builder.Property(t => t.RiskMin).IsRequired();
        builder.Property(t => t.RiskMax).IsRequired();
        builder.Property(t => t.RequiresSpeculativeUnlock).IsRequired();

        builder.Property(t => t.BucketsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(t => t.RebalanceCadence)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.DrawdownAlertPct).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
