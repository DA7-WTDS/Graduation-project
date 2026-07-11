using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Modules.Portfolio.Domain.Instruments;

namespace Project.Modules.Portfolio.Infrastructure.Instruments;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Market)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(i => i.Symbol)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => new { i.Market, i.Symbol }).IsUnique();

        builder.Property(i => i.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.AssetClass)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(i => i.Sector)
            .HasMaxLength(100);

        // Postgres text[] — sleeve tokens from Sleeves.
        builder.Property(i => i.SuitableFor)
            .IsRequired();

        builder.Property(i => i.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(i => i.IsActive).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
    }
}
