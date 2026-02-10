using Project.Modules.Portfolio.Domain.Portfolios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

internal sealed class PortfolioConfiguration : IEntityTypeConfiguration<Domain.Portfolios.Portfolio>
{
    public void Configure(EntityTypeBuilder<Domain.Portfolios.Portfolio> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.PrimaryGoal)
            .IsRequired();

        builder.Property(p => p.TimeHorizon)
            .IsRequired();

        builder.Property(p => p.RiskTolerance)
            .IsRequired();

        builder.Property(p => p.MarketReaction)
            .IsRequired();

        builder.Property(p => p.InvestmentExperience)
            .IsRequired();

        builder.Property(p => p.StocksPercentage)
            .IsRequired();

        builder.Property(p => p.BondsPercentage)
            .IsRequired();

        builder.Property(p => p.EtfsPercentage)
            .IsRequired();

        builder.Property(p => p.CashPercentage)
            .IsRequired();

        builder.Property(p => p.RiskProfile)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
