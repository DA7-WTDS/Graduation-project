using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Portfolio.Domain.Shadow;

namespace Project.Modules.Portfolio.Infrastructure.Shadow;

internal sealed class ShadowPortfolioConfiguration : IEntityTypeConfiguration<ShadowPortfolio>
{
    public void Configure(EntityTypeBuilder<ShadowPortfolio> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TemplateKey).HasMaxLength(60).IsRequired();
        builder.Property(p => p.TemplateName).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Market).HasMaxLength(10).IsRequired();
        builder.Property(p => p.RiskBand).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.RebalanceCadence).HasMaxLength(20).IsRequired();
        builder.Property(p => p.DrawdownAlertPct).IsRequired();
        builder.Property(p => p.Notional).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.CashBalance).IsRequired();
        builder.Property(p => p.LastNav).IsRequired();
        builder.Property(p => p.HighWaterMarkNav).IsRequired();
        builder.Property(p => p.InceptionDate).IsRequired();
        builder.Property(p => p.LastValuedOn).IsRequired(false);
        builder.Property(p => p.LastRebalancedOn).IsRequired(false);
        builder.Property(p => p.DrawdownAlertActive).IsRequired();

        // One shadow portfolio per template per market.
        builder.HasIndex(p => new { p.Market, p.TemplateKey }).IsUnique();

        builder.HasMany(p => p.Positions)
            .WithOne()
            .HasForeignKey(pos => pos.ShadowPortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Positions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ShadowPositionConfiguration : IEntityTypeConfiguration<ShadowPosition>
{
    public void Configure(EntityTypeBuilder<ShadowPosition> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ShadowPortfolioId).IsRequired();
        builder.Property(p => p.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Sleeve).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Shares).IsRequired();
        builder.Property(p => p.AvgCost).IsRequired();
    }
}

internal sealed class ShadowSnapshotConfiguration : IEntityTypeConfiguration<ShadowSnapshot>
{
    public void Configure(EntityTypeBuilder<ShadowSnapshot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShadowPortfolioId).IsRequired();
        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.Nav).IsRequired();
        builder.Property(s => s.DailyReturn).IsRequired();
        builder.Property(s => s.Rebalanced).IsRequired();

        // One snapshot per portfolio per day; the series is read most-recent-first.
        builder.HasIndex(s => new { s.ShadowPortfolioId, s.Date }).IsUnique();
    }
}
