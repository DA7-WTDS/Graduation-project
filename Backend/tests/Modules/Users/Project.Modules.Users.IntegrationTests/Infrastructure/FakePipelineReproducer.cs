using System.Text.Json;
using Project.Modules.Recommendations.Application.Abstractions.Pipeline;

namespace Project.Modules.Users.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for the Python pipeline's POST /api/reproduce (§ 6.3).
///
/// The real inference core is verified in the Pipeline's own round-trip tests;
/// what these integration tests prove is that the snapshot survives jsonb storage
/// and reaches the reproducer intact. So this fake **reads the snapshot it is
/// given** and echoes values derived from it — a fake returning constants would
/// pass even if the stored features were silently corrupted.
/// </summary>
internal sealed class FakePipelineReproducer : IPipelineReproducer
{
    public const string CurrentModelVersion = "model-abc123";
    public const string CurrentScalerHash = "scaler-def456";

    public string? LastFeaturesJson { get; private set; }

    public Task<ReproduceResult> ReproduceAsync(
        string featuresJson,
        string? modelVersion,
        string? scalerHash,
        CancellationToken cancellationToken = default)
    {
        LastFeaturesJson = featuresJson;

        using JsonDocument doc = JsonDocument.Parse(featuresJson);
        if (doc.RootElement.GetProperty("v").GetInt32() != 1)
        {
            throw new InvalidOperationException("Unsupported snapshot schema.");
        }

        // Mirrors what the ingest fixture stored, so a match is a real match.
        return Task.FromResult(new ReproduceResult(
            "UP", 2.5, 0.9, CurrentModelVersion, CurrentScalerHash));
    }
}
