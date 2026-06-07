using System;
using System.Threading;
using System.Threading.Tasks;

namespace Project.Modules.Recommendations.PublicApi;

public interface IRecommendationsApi
{
    Task<Guid?> GetLatestDailyRunIdAsync(CancellationToken cancellationToken = default);
}
