using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Infrastructure.DailyRuns;

internal sealed class StockPredictionConfiguration : IEntityTypeConfiguration<StockPrediction>
{
    public void Configure(EntityTypeBuilder<StockPrediction> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DailyRunId).IsRequired();
        builder.Property(p => p.Ticker).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Direction).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Signal).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Agreement).IsRequired().HasMaxLength(20);
        builder.Property(p => p.RiskLevel).IsRequired().HasMaxLength(10);
        builder.Property(p => p.RatingLabel).HasMaxLength(40);
        builder.Property(p => p.Rationale).IsRequired();
        builder.Property(p => p.RiskFlags).HasColumnType("text[]");

        // Audit snapshot (§ 6.3). ~3.4 KB/prediction; jsonb keeps it queryable
        // if we ever need to mine stored inputs. Ignore the derived flag.
        builder.Property(p => p.FeaturesJson).HasColumnType("jsonb");
        builder.Property(p => p.ModelVersion).HasMaxLength(64);
        builder.Property(p => p.ScalerHash).HasMaxLength(64);
        builder.Ignore(p => p.IsReproducible);

        builder.HasIndex(p => p.Ticker);
        builder.HasIndex(p => p.RiskLevel);
        // Answers "which predictions came from artifact X?" — the question you
        // ask the moment a model change is suspected.
        builder.HasIndex(p => p.ModelVersion);
    }
}
