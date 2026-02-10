using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Infrastructure.Portfolios;
using Project.Common.Infrastructure.Outbox;
using Project.Common.Infrastructure.Inbox;

namespace Project.Modules.Portfolio.Infrastructure.Database;

public class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<Domain.Portfolios.Portfolio> Portfolios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Portfolio);
        modelBuilder.ApplyConfiguration(new PortfolioConfiguration());

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
    }
}
