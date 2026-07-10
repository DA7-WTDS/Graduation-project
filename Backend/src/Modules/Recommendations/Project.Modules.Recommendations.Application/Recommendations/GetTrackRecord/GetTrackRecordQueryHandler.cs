using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.Abstractions.Outcomes;

namespace Project.Modules.Recommendations.Application.Recommendations.GetTrackRecord;

internal sealed class GetTrackRecordQueryHandler(IPredictionOutcomeRepository outcomeRepository)
    : IQueryHandler<GetTrackRecordQuery, TrackRecordResponse>
{
    private static readonly int[] windowsDays = [30, 90, 365];

    public async Task<Result<TrackRecordResponse>> Handle(GetTrackRecordQuery request, CancellationToken cancellationToken)
    {
        DateTime since = DateTime.UtcNow.AddDays(-windowsDays.Max());
        IReadOnlyList<OutcomeStat> outcomes = await outcomeRepository.GetSinceAsync(since, cancellationToken);

        if (outcomes.Count == 0)
        {
            // Empty state, not an error — the page renders "no history yet".
            return Result.Ok(new TrackRecordResponse(0, null, null, []));
        }

        var windows = new List<WindowStats>();
        foreach (int days in windowsDays)
        {
            DateTime windowStart = DateTime.UtcNow.AddDays(-days);
            var inWindow = outcomes.Where(o => o.RunGeneratedAt >= windowStart).ToList();
            if (inWindow.Count == 0)
            {
                windows.Add(new WindowStats(days, 0, 0, 0, []));
                continue;
            }

            var byRisk = inWindow
                .GroupBy(o => o.RiskLevel)
                .OrderBy(g => g.Key)
                .Select(g => new RiskBucketStats(
                    g.Key,
                    g.Count(),
                    HitRate(g.ToList()),
                    AvgReturn(g.ToList())))
                .ToList();

            windows.Add(new WindowStats(days, inWindow.Count, HitRate(inWindow), AvgReturn(inWindow), byRisk));
        }

        return Result.Ok(new TrackRecordResponse(
            outcomes.Count,
            outcomes.Min(o => o.RunGeneratedAt),
            outcomes.Max(o => o.RunGeneratedAt),
            windows));
    }

    private static double HitRate(IReadOnlyList<OutcomeStat> stats) =>
        Math.Round(stats.Count(s => s.DirectionHit) * 100.0 / stats.Count, 2);

    private static double AvgReturn(IReadOnlyList<OutcomeStat> stats) =>
        Math.Round(stats.Average(s => s.RealizedReturnPct), 4);
}
