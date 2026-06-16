using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Recommendations.Domain.Holdings;

namespace Project.Modules.Recommendations.Infrastructure.Holdings;

internal sealed class UserHoldingConfiguration : IEntityTypeConfiguration<UserHolding>
{
    public void Configure(EntityTypeBuilder<UserHolding> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.UserId).IsRequired();
        builder.Property(h => h.Ticker).IsRequired();
        builder.Property(h => h.AllocationPct).IsRequired();
        builder.Property(h => h.RunGeneratedAt).IsRequired();
        builder.Property(h => h.UpdatedAt).IsRequired();

        builder.HasIndex(h => h.UserId);
    }
}
