using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Strategies;
using Project.Modules.Portfolio.Domain.Proposals;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Infrastructure.Portfolios;
using Project.Modules.Portfolio.Infrastructure.Goals;
using Project.Modules.Portfolio.Infrastructure.Instruments;
using Project.Modules.Portfolio.Infrastructure.Strategies;
using Project.Modules.Portfolio.Infrastructure.Proposals;
using Project.Common.Infrastructure.Outbox;
using Project.Common.Infrastructure.Inbox;

namespace Project.Modules.Portfolio.Infrastructure.Database;

public class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<Goal> Goals { get; set; }
    internal DbSet<QuestionnaireResponse> QuestionnaireResponses { get; set; }
    internal DbSet<InvestorProfile> InvestorProfiles { get; set; }
    internal DbSet<Instrument> Instruments { get; set; }
    internal DbSet<StrategyTemplate> StrategyTemplates { get; set; }
    internal DbSet<PortfolioProposal> PortfolioProposals { get; set; }
    internal DbSet<GoalPortfolio> GoalPortfolios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Portfolio);
        modelBuilder.ApplyConfiguration(new GoalConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionnaireResponseConfiguration());
        modelBuilder.ApplyConfiguration(new InvestorProfileConfiguration());
        modelBuilder.ApplyConfiguration(new InstrumentConfiguration());
        modelBuilder.ApplyConfiguration(new StrategyTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new PortfolioProposalConfiguration());
        modelBuilder.ApplyConfiguration(new GoalPortfolioConfiguration());
        modelBuilder.ApplyConfiguration(new PortfolioHoldingConfiguration());

        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
    }
}
