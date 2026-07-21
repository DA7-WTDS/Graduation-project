using Project.Modules.Portfolio.Domain.Shadow;

namespace Project.Modules.Portfolio.Application.Abstractions.Shadow;

public interface IShadowPortfolioRepository
{
    /// <summary>All shadow portfolios for a market, with positions — the nightly
    /// job's working set.</summary>
    Task<IReadOnlyList<ShadowPortfolio>> GetAllForMarketAsync(string market, CancellationToken cancellationToken = default);

    Task AddAsync(ShadowPortfolio portfolio, CancellationToken cancellationToken = default);

    Task AddSnapshotAsync(ShadowSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Whether a snapshot already exists for this portfolio on this date
    /// — the nightly job is idempotent across re-runs of the same day.</summary>
    Task<bool> SnapshotExistsAsync(Guid portfolioId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Every portfolio's full NAV series (oldest first) — the public
    /// track-record read.</summary>
    Task<IReadOnlyList<ShadowSnapshot>> GetAllSnapshotsAsync(string market, CancellationToken cancellationToken = default);
}
