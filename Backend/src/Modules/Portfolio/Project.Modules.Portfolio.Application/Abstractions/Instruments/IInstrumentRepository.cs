using Project.Modules.Portfolio.Domain.Instruments;

namespace Project.Modules.Portfolio.Application.Abstractions.Instruments;

public interface IInstrumentRepository
{
    Task<IReadOnlyList<Instrument>> GetActiveByMarketAsync(string market, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Instrument>> GetAllByMarketAsync(string market, CancellationToken cancellationToken = default);
    Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default);
}
