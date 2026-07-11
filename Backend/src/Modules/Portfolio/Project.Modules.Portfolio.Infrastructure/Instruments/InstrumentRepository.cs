using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Infrastructure.Database;

namespace Project.Modules.Portfolio.Infrastructure.Instruments;

internal sealed class InstrumentRepository(PortfolioDbContext dbContext) : IInstrumentRepository
{
    public async Task<IReadOnlyList<Instrument>> GetActiveByMarketAsync(string market, CancellationToken cancellationToken = default)
    {
        return await dbContext.Instruments
            .Where(i => i.Market == market && i.IsActive)
            .OrderBy(i => i.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Instrument>> GetAllByMarketAsync(string market, CancellationToken cancellationToken = default)
    {
        return await dbContext.Instruments
            .Where(i => i.Market == market)
            .OrderBy(i => i.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default)
    {
        await dbContext.Instruments.AddAsync(instrument, cancellationToken);
    }
}
