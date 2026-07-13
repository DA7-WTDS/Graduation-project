using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Proposals;

namespace Project.Modules.Portfolio.Infrastructure.Proposals;

internal sealed class PortfolioProposalConfiguration : IEntityTypeConfiguration<PortfolioProposal>
{
    public void Configure(EntityTypeBuilder<PortfolioProposal> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.GoalId).IsRequired();

        builder.HasOne<Goal>()
            .WithMany()
            .HasForeignKey(p => p.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Immutable versioning: one proposal per (goal, version).
        builder.HasIndex(p => new { p.GoalId, p.Version }).IsUnique();

        builder.Property(p => p.TemplateKey).HasMaxLength(50).IsRequired();
        builder.Property(p => p.TemplateName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RebalanceCadence).HasMaxLength(20).IsRequired();
        builder.Property(p => p.DrawdownAlertPct).IsRequired();

        builder.Property(p => p.RiskBand)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.EffectiveRisk).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();

        builder.Property(p => p.PositionsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(p => p.AssumptionsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(p => p.InputsHash).HasMaxLength(64).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.AcceptedAt).IsRequired(false);
    }
}
