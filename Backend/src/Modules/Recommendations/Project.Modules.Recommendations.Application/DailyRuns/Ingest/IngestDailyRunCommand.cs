using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.DailyRuns.Ingest;

/// <summary>
/// <paramref name="GatesPassed"/> carries the pipeline's § 6.2 quality-gate
/// verdict: false lands the run as Quarantined regardless of approval mode,
/// with <paramref name="GateFailures"/> as the recorded reason.
///
/// <paramref name="Simulated"/> marks a § C point-in-time replay: it lands as
/// <c>DailyRunStatus.Simulated</c>, which is never servable and cannot be promoted,
/// and raises no ingest event — a year-long backfill would otherwise deliver several
/// hundred ops alerts.
/// </summary>
public sealed record IngestDailyRunCommand(
    DateTime GeneratedAt,
    IReadOnlyList<PredictionRecordDto> Records,
    bool GatesPassed = true,
    IReadOnlyList<string>? GateFailures = null,
    string Market = "us",
    bool Simulated = false) : ICommand<Guid>;
