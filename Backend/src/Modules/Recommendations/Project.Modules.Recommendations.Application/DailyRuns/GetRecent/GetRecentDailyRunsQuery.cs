using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.DailyRuns.UpdateStatus;

namespace Project.Modules.Recommendations.Application.DailyRuns.GetRecent;

/// <summary>Operator view for the § 6.2 kill switch: recent runs with status.</summary>
public sealed record GetRecentDailyRunsQuery(int Take = 20) : IQuery<IReadOnlyList<DailyRunStatusResponse>>;
