using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Recommendations.Domain.Outcomes;

namespace Project.Modules.Recommendations.Infrastructure.Outcomes;

internal sealed class PredictionOutcomeConfiguration : IEntityTypeConfiguration<PredictionOutcome>
{
    public void Configure(EntityTypeBuilder<PredictionOutcome> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Ticker).IsRequired().HasMaxLength(20);
        builder.Property(o => o.PredictedDirection).IsRequired().HasMaxLength(10);
        builder.Property(o => o.RiskLevel).IsRequired().HasMaxLength(10);

        // One outcome per prediction — the job's idempotency guarantee.
        builder.HasIndex(o => o.StockPredictionId).IsUnique();

        // Rolling-metrics queries filter by when the run happened.
        builder.HasIndex(o => o.RunGeneratedAt);
    }
}
