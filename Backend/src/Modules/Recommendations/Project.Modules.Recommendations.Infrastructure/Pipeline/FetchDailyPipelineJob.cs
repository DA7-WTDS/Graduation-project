using MediatR;
using Microsoft.Extensions.Logging;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Quartz;
using System.Net.Http.Json;

namespace Project.Modules.Recommendations.Infrastructure.Pipeline;

/// <summary>
/// Daily Quartz job that orchestrates the ML scoring pipeline.
///
/// Flow:
///   1. POST http://localhost:8000/api/score  (Python unified pipeline service)
///   2. Deserialise the scored records (matches PredictionRecordDto schema)
///   3. Dispatch IngestDailyRunCommand in-process via MediatR (no HTTP round-trip)
///
/// The Python service (POST /api/score) runs the full chain internally:
///   ticker fetch → LSTM+XGBoost prediction → FinBERT+analyst sentiment → risk rules.
///
/// Authentication: none required — the Python service runs on localhost and is not
/// internet-exposed. The ingest is dispatched via MediatR, bypassing the
/// POST /api/internal/daily-results HTTP endpoint entirely.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class FetchDailyPipelineJob(
    HttpClient httpClient,
    ISender sender,
    ILogger<FetchDailyPipelineJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("FetchDailyPipelineJob — starting daily pipeline run.");

        ScoreResponse payload;

        try
        {
            // POST /api/score — no body needed; the Python service fetches tickers internally.
            HttpResponseMessage response = await httpClient.PostAsync(
                "/api/score",
                content: null,
                context.CancellationToken);

            response.EnsureSuccessStatusCode();

            payload = await response.Content.ReadFromJsonAsync<ScoreResponse>(
                cancellationToken: context.CancellationToken)
                ?? throw new InvalidOperationException("Python /api/score returned null payload.");
        }
        catch (Exception ex)
        {
            // Log and return — Quartz will reschedule on the next cron tick.
            // Throwing here would cause Quartz to mark the trigger as ERROR and
            // potentially stop future executions depending on the misfire policy.
            logger.LogError(ex, "FetchDailyPipelineJob — failed to call Python /api/score.");
            return;
        }

        logger.LogInformation(
            "FetchDailyPipelineJob — received {Count} scored records from pipeline (generated at {GeneratedAt}).",
            payload.Count,
            payload.GeneratedAt);

        bool gatesPassed = !string.Equals(payload.Status, "quarantined", StringComparison.OrdinalIgnoreCase);
        if (!gatesPassed)
        {
            logger.LogError(
                "FetchDailyPipelineJob — pipeline quality gates FAILED; run will be quarantined: {Failures}",
                string.Join("; ", payload.GateFailures));
        }

        // Dispatch in-process — MediatR, no HTTP.
        // IngestDailyRunCommand is idempotent on GeneratedAt (same timestamp = returns existing run id).
        var result = await sender.Send(
            new IngestDailyRunCommand(payload.GeneratedAt, payload.Records, gatesPassed, payload.GateFailures),
            context.CancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "FetchDailyPipelineJob — ingested successfully. RunId={RunId}, Records={Count}.",
                result.Value,
                payload.Count);
        }
        else
        {
            logger.LogError(
                "FetchDailyPipelineJob — IngestDailyRunCommand failed: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
        }
    }
}
