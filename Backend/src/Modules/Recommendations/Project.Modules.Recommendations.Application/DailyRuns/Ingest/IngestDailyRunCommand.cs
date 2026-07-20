using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.DailyRuns.Ingest;

/// <summary>
/// <paramref name="GatesPassed"/> carries the pipeline's § 6.2 quality-gate
/// verdict: false lands the run as Quarantined regardless of approval mode,
/// with <paramref name="GateFailures"/> as the recorded reason.
/// </summary>
public sealed record IngestDailyRunCommand(
    DateTime GeneratedAt,
    IReadOnlyList<PredictionRecordDto> Records,
    bool GatesPassed = true,
    IReadOnlyList<string>? GateFailures = null) : ICommand<Guid>;
