namespace Project.Modules.Recommendations.Application.Abstractions.Pipeline;

/// <summary>
/// Replays a stored § 6.3 feature snapshot through the pipeline's inference core.
/// Lives behind an abstraction so the audit handler is testable without the
/// Python service running.
/// </summary>
public interface IPipelineReproducer
{
    Task<ReproduceResult> ReproduceAsync(
        string featuresJson,
        string? modelVersion,
        string? scalerHash,
        CancellationToken cancellationToken = default);
}

public sealed record ReproduceResult(
    string Direction,
    double ChangePct,
    double Confidence,
    string ModelVersion,
    string ScalerHash);
