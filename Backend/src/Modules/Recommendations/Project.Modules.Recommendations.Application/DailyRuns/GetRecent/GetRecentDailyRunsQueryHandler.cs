using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.DailyRuns.UpdateStatus;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.DailyRuns.GetRecent;

internal sealed class GetRecentDailyRunsQueryHandler(IDailyRunRepository dailyRunRepository)
    : IQueryHandler<GetRecentDailyRunsQuery, IReadOnlyList<DailyRunStatusResponse>>
{
    public async Task<Result<IReadOnlyList<DailyRunStatusResponse>>> Handle(
        GetRecentDailyRunsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<DailyRun> runs = await dailyRunRepository.GetRecentAsync(request.Take, cancellationToken);

        IReadOnlyList<DailyRunStatusResponse> response = runs
            .Select(r => new DailyRunStatusResponse(
                r.Id, r.GeneratedAt, r.Count, r.Status.ToString(), r.StatusReason, r.StatusChangedAt))
            .ToList();

        return Result.Ok(response);
    }
}
