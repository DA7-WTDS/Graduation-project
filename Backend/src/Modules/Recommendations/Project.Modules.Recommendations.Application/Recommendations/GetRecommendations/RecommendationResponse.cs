using System.Text.Json.Serialization;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

public sealed record RecommendationResponse(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("picks")] IReadOnlyList<RecommendationItem> Picks,
    [property: JsonPropertyName("generated_at")] DateTime GeneratedAt);

public sealed record RecommendationItem(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("allocation_pct")] double AllocationPct,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("risk_note")] string RiskNote,
    [property: JsonPropertyName("fit")] string Fit);

/// <summary>Deserialization target for the raw LLM output (summary + picks only).</summary>
internal sealed class LlmRecommendationResult
{
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("picks")] public List<RecommendationItem> Picks { get; set; } = [];
}
