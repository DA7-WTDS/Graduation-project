using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Project.Modules.Recommendations.Application.Abstractions.Pipeline;

namespace Project.Modules.Recommendations.Infrastructure.Pipeline;

/// <summary>
/// Replays a stored feature snapshot through the pipeline's POST /api/reproduce
/// (§ 6.3). The pipeline replays it through the same inference core live scoring
/// uses, so a mismatch means the model really changed.
/// </summary>
internal sealed class PipelineReproducer(HttpClient httpClient) : IPipelineReproducer
{
    public async Task<ReproduceResult> ReproduceAsync(
        string featuresJson,
        string? modelVersion,
        string? scalerHash,
        CancellationToken cancellationToken = default)
    {
        // features is stored jsonb — hand it back as raw JSON, never re-modelled.
        using JsonDocument features = JsonDocument.Parse(featuresJson);

        var payload = new ReproduceRequestDto
        {
            Features = features.RootElement,
            ModelVersion = modelVersion,
            ScalerHash = scalerHash,
        };

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/reproduce", payload, cancellationToken);

        response.EnsureSuccessStatusCode();

        ReproduceResponseDto dto = await response.Content.ReadFromJsonAsync<ReproduceResponseDto>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Pipeline /api/reproduce returned an empty body.");

        return new ReproduceResult(
            dto.Direction, dto.ChangePct, dto.Confidence, dto.ModelVersion, dto.ScalerHash);
    }

    private sealed record ReproduceRequestDto
    {
        [JsonPropertyName("features")]      public JsonElement Features { get; init; }
        [JsonPropertyName("model_version")] public string? ModelVersion { get; init; }
        [JsonPropertyName("scaler_hash")]   public string? ScalerHash { get; init; }
    }

    private sealed record ReproduceResponseDto
    {
        [JsonPropertyName("direction")]     public string Direction { get; init; } = string.Empty;
        [JsonPropertyName("change_pct")]    public double ChangePct { get; init; }
        [JsonPropertyName("confidence")]    public double Confidence { get; init; }
        [JsonPropertyName("model_version")] public string ModelVersion { get; init; } = string.Empty;
        [JsonPropertyName("scaler_hash")]   public string ScalerHash { get; init; } = string.Empty;
    }
}
