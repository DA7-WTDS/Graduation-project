using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.DailyRuns.UpdateStatus;

/// <summary>§ 6.2 kill switch: operator flips a run's lifecycle state.</summary>
public sealed record UpdateDailyRunStatusCommand(
    Guid RunId,
    DailyRunStatus Target,
    string? Reason) : ICommand<DailyRunStatusResponse>;

public sealed record DailyRunStatusResponse(
    Guid RunId,
    DateTime GeneratedAt,
    int Count,
    string Status,
    string? StatusReason,
    DateTime StatusChangedAt);
