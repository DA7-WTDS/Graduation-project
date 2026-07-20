using System.Text.Json.Serialization;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;

namespace Project.Modules.Recommendations.Infrastructure.Pipeline;

/// <summary>
/// Deserialisation target for the Python POST /api/score response.
/// Snake_case JSON property names match the Python service output.
/// </summary>
internal sealed record ScoreResponse
{
    [JsonPropertyName("generated_at")]  public DateTime GeneratedAt { get; init; }
    [JsonPropertyName("count")]         public int Count { get; init; }
    [JsonPropertyName("records")]       public IReadOnlyList<PredictionRecordDto> Records { get; init; } = [];

    // § 6.2 quality-gate verdict: "ok" or "quarantined". Defaults keep older
    // pipeline builds (no gate fields) publishing as before.
    [JsonPropertyName("status")]        public string Status { get; init; } = "ok";
    [JsonPropertyName("gate_failures")] public IReadOnlyList<string> GateFailures { get; init; } = [];
}
