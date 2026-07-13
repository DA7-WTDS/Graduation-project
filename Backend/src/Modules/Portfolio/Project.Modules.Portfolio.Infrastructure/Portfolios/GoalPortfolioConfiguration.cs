using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

internal sealed class GoalPortfolioConfiguration : IEntityTypeConfiguration<GoalPortfolio>
{
    public void Configure(EntityTypeBuilder<GoalPortfolio> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.GoalId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.ProposalId).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.DrawdownThreshold).IsRequired();
        builder.Property(p => p.HighWaterMarkNav).IsRequired();
        builder.Property(p => p.LastNav).IsRequired();
        builder.Property(p => p.LastValuedAt).IsRequired(false);
        builder.Property(p => p.DrawdownAlertActive).IsRequired();
        builder.Property(p => p.DriftAlertActive).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.InceptionDate).IsRequired();
        builder.Property(p => p.ClosedAt).IsRequired(false);

        // One active portfolio per goal (closed ones stay for history).
        builder.HasIndex(p => new { p.GoalId, p.Status });

        builder.HasMany(p => p.Holdings)
            .WithOne()
            .HasForeignKey(h => h.GoalPortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Holdings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PortfolioHoldingConfiguration : IEntityTypeConfiguration<PortfolioHolding>
{
    public void Configure(EntityTypeBuilder<PortfolioHolding> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.GoalPortfolioId).IsRequired();
        builder.Property(h => h.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(h => h.Sleeve).HasMaxLength(20).IsRequired();
        builder.Property(h => h.TargetWeight).IsRequired();
        builder.Property(h => h.EntryPrice).IsRequired();
        builder.Property(h => h.Shares).IsRequired();
    }
}
