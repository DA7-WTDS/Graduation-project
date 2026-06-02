using Microsoft.EntityFrameworkCore;
using Project.Common.Infrastructure.Inbox;
using Project.Common.Infrastructure.Outbox;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.DailyRuns;

namespace Project.Modules.Recommendations.Infrastructure.Database;

public class RecommendationsDbContext(DbContextOptions<RecommendationsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<DailyRun> DailyRuns { get; set; }
    internal DbSet<StockPrediction> StockPredictions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Recommendations);

        modelBuilder.ApplyConfiguration(new DailyRunConfiguration());
        modelBuilder.ApplyConfiguration(new StockPredictionConfiguration());

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
    }
}
