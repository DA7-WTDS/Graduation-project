using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Instruments;

public enum InstrumentType
{
    Stock,
    Etf,
    Fund,
    MmFund
}

public enum AssetClass
{
    Equity,
    Gold,
    FixedIncome,
    CashLike
}

/// <summary>Sleeve tokens for <see cref="Instrument.SuitableFor"/> (§ 3.4).
/// Stored as a text[] so template rules stay data, not code.</summary>
public static class Sleeves
{
    public const string Stability = "stability";
    public const string Core = "core";
    public const string Tactical = "tactical";
    public const string Speculative = "speculative";
}

/// <summary>
/// The instrument registry (§ 3.1) — where "context-awareness" lives. Strategy
/// templates and the allocation optimizer read everything from here; no symbol,
/// volatility number, or liquidity threshold is ever hard-coded downstream.
/// Computed stats (vol, liquidity) are refreshed nightly from the pipeline.
/// </summary>
public sealed class Instrument : Entity
{
    private Instrument() { }

    public Guid Id { get; private set; }
    public string Market { get; private set; }          // "us" | "egx"
    public string Symbol { get; private set; }
    public InstrumentType Type { get; private set; }
    public AssetClass AssetClass { get; private set; }
    public string Currency { get; private set; }
    public string? Sector { get; private set; }
    public List<string> SuitableFor { get; private set; } = [];

    public double? RealizedVol1Y { get; private set; }
    public double? AvgDailyValueTraded { get; private set; }
    public double? LastClose { get; private set; }
    public DateTime? StatsAsOf { get; private set; }

    public bool IsActive { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Instrument Create(
        string market,
        string symbol,
        InstrumentType type,
        AssetClass assetClass,
        string currency,
        IEnumerable<string> suitableFor,
        string? sector = null,
        string? metadataJson = null)
    {
        return new Instrument
        {
            Id = Guid.NewGuid(),
            Market = market,
            Symbol = symbol,
            Type = type,
            AssetClass = assetClass,
            Currency = currency,
            Sector = sector,
            SuitableFor = suitableFor.Distinct().ToList(),
            IsActive = true,
            MetadataJson = metadataJson,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStats(double? realizedVol1Y, double? avgDailyValueTraded, double? lastClose, string? sector, DateTime asOf)
    {
        // Nulls mean "no data this run" — keep the previous value rather than
        // blanking a stat because of a one-night vendor gap.
        RealizedVol1Y = realizedVol1Y ?? RealizedVol1Y;
        AvgDailyValueTraded = avgDailyValueTraded ?? AvgDailyValueTraded;
        LastClose = lastClose ?? LastClose;
        if (!string.IsNullOrEmpty(sector) && sector != "Unknown")
        {
            Sector = sector;
        }

        StatsAsOf = asOf;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
