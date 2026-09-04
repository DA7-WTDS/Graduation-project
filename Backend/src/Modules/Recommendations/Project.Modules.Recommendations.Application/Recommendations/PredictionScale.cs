using System.Text.Json;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.Recommendations;

/// <summary>
/// What <see cref="StockPrediction.ChangePct"/> actually means for a given run.
///
/// The pipeline serves one of two stacks (MVP_PLAN § A) and they disagree about this
/// field: <c>SERVING_MODEL=trees</c> (the champion, and the default) emits a return
/// RELATIVE to the universe median, while the legacy <c>hybrid</c> rollback emits an
/// absolute 30-day return. Presenting one as the other is not a wording nit — it turns
/// a ranking signal into a price forecast, which is the one claim this product is
/// careful never to make.
///
/// Rather than a config flag the backend would have to be told about (and would
/// eventually forget to update), the scale is read from the § 6.3 feature snapshot the
/// pipeline stamps onto every prediction. A rollback therefore re-labels the UI and the
/// LLM prompt by itself, from the data, with nothing to remember.
/// </summary>
internal static class PredictionScale
{
    /// <summary>Wire value for the predictions API.</summary>
    public const string Relative = "relative";

    /// <summary>Wire value for the predictions API.</summary>
    public const string Absolute = "absolute";

    /// <summary>
    /// True when ChangePct is relative to the universe median. Absent or unreadable
    /// snapshot ⇒ absolute, matching the pipeline's own convention (rows stored before
    /// § 6.3 shipped came from the hybrid stack). Never throws: a malformed blob must
    /// not be able to fail a recommendation or a dashboard read.
    /// </summary>
    public static bool IsRelative(IEnumerable<StockPrediction> predictions)
    {
        foreach (StockPrediction p in predictions)
        {
            if (string.IsNullOrWhiteSpace(p.FeaturesJson))
            {
                continue;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(p.FeaturesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("mode", out JsonElement mode)
                    && mode.ValueKind == JsonValueKind.String)
                {
                    return string.Equals(mode.GetString(), "trees", StringComparison.OrdinalIgnoreCase);
                }

                return false; // snapshot present but no mode ⇒ legacy hybrid row
            }
            catch (JsonException)
            {
                // Unreadable blob: try the next prediction rather than guessing.
            }
        }

        return false;
    }

    /// <summary>The scale as the wire value the frontend switches its copy on.</summary>
    public static string Of(IEnumerable<StockPrediction> predictions) =>
        IsRelative(predictions) ? Relative : Absolute;
}
