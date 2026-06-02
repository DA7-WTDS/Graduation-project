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

        builder.HasIndex(p => p.Ticker);
        builder.HasIndex(p => p.RiskLevel);
    }
}
