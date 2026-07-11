using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Portfolio.Domain.Goals;

namespace Project.Modules.Portfolio.Infrastructure.Goals;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.UserId).IsRequired();
        builder.HasIndex(g => g.UserId);

        builder.Property(g => g.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(g => g.HorizonYears).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired(false);
    }
}

internal sealed class QuestionnaireResponseConfiguration : IEntityTypeConfiguration<QuestionnaireResponse>
{
    public void Configure(EntityTypeBuilder<QuestionnaireResponse> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.GoalId).IsRequired();
        builder.HasIndex(q => q.GoalId);

        builder.HasOne<Goal>()
            .WithMany()
            .HasForeignKey(q => q.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Raw answers stored verbatim as jsonb — the immutable suitability record.
        builder.Property(q => q.AnswersJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(q => q.ScoringVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(q => q.SubmittedAt).IsRequired();
    }
}

internal sealed class InvestorProfileConfiguration : IEntityTypeConfiguration<InvestorProfile>
{
    public void Configure(EntityTypeBuilder<InvestorProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.GoalId).IsRequired();

        builder.HasOne<Goal>()
            .WithMany()
            .HasForeignKey(p => p.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<QuestionnaireResponse>()
            .WithMany()
            .HasForeignKey(p => p.QuestionnaireResponseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Append-only versioning: one profile row per (goal, version).
        builder.HasIndex(p => new { p.GoalId, p.Version }).IsUnique();

        builder.Property(p => p.Capacity).IsRequired();
        builder.Property(p => p.Tolerance).IsRequired();
        builder.Property(p => p.EffectiveRisk).IsRequired();

        builder.Property(p => p.RiskBand)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Engagement)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.UsdComfort)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.SpeculativeUnlocked).IsRequired();

        builder.Property(p => p.ScoringVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
    }
}
