using Project.Common.Domain.Abstractions;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.Abstractions.DailyRuns;

public interface IDailyRunRepository : IRepository<DailyRun>
{
    Task<DailyRun?> GetLatestAsync(bool includePredictions = false, CancellationToken cancellationToken = default);
    Task<DailyRun?> GetByGeneratedAtAsync(DateTime generatedAt, CancellationToken cancellationToken = default);
}
