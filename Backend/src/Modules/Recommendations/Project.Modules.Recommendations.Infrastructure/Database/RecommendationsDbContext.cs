using Microsoft.EntityFrameworkCore;
using Project.Common.Infrastructure.Inbox;
using Project.Common.Infrastructure.Outbox;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Domain.Holdings;
using Project.Modules.Recommendations.Domain.Outcomes;
using Project.Modules.Recommendations.Infrastructure.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Holdings;
using Project.Modules.Recommendations.Infrastructure.Outcomes;

namespace Project.Modules.Recommendations.Infrastructure.Database;

public class RecommendationsDbContext(DbContextOptions<RecommendationsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<DailyRun> DailyRuns { get; set; }
    internal DbSet<StockPrediction> StockPredictions { get; set; }
    internal DbSet<UserHolding> UserHoldings { get; set; }
    internal DbSet<PredictionOutcome> PredictionOutcomes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Recommendations);

        modelBuilder.ApplyConfiguration(new DailyRunConfiguration());
        modelBuilder.ApplyConfiguration(new StockPredictionConfiguration());
        modelBuilder.ApplyConfiguration(new UserHoldingConfiguration());
        modelBuilder.ApplyConfiguration(new PredictionOutcomeConfiguration());

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
    }
}
