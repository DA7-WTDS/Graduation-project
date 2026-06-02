using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.DailyRuns.Ingest;

public sealed record IngestDailyRunCommand(
    DateTime GeneratedAt,
    IReadOnlyList<PredictionRecordDto> Records) : ICommand<Guid>;
