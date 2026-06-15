using System.Globalization;
using System.Text;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.Domain.DailyRuns;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

internal static class RecommendationPrompt
{
    /// <summary>JSON schema the native Gemini endpoint constrains the output to (guaranteed valid JSON).</summary>
    public const string ResponseSchema = """
    {
      "type": "object",
      "properties": {
        "summary": { "type": "string" },
        "picks": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "ticker": { "type": "string" },
              "action": { "type": "string", "enum": ["BUY", "SELL", "HOLD"] },
              "allocation_pct": { "type": "number" },
              "reason": { "type": "string" },
              "risk_note": { "type": "string" },
              "fit": { "type": "string" }
            },
            "required": ["ticker", "action", "allocation_pct", "reason", "risk_note", "fit"],
            "propertyOrdering": ["ticker", "action", "allocation_pct", "reason", "risk_note", "fit"]
          }
        }
      },
      "required": ["summary", "picks"],
      "propertyOrdering": ["summary", "picks"]
    }
    """;

    public const string System =
        "You are QuantWise's investment recommendation assistant. You are a CONSTRAINED SYNTHESISER, not a predictor.\n" +
        "Rules:\n" +
        "1. Use ONLY the data in the user message. Never invent tickers, prices, or numbers.\n" +
        "2. Respect the risk grading: do NOT recommend HIGH-risk stocks, or ones flagged signal_contradiction or internal_conflict, to Conservative users; be cautious for Moderate; Aggressive may include higher-risk ideas.\n" +
        "3. Tailor selections to the user's risk profile and target allocation.\n" +
        "4. Choose the best 5-8 stocks that fit THIS user and briefly justify each using the provided fields.\n" +
        "5. Assign each pick an allocation_pct: the share (percent) of the user's STOCK allocation to put in that stock. " +
        "The allocation_pct values across all picks MUST sum to 100. Weight by conviction and fit; diversify more evenly " +
        "for Conservative profiles, allow more concentration for Aggressive.\n" +
        "6. Output ONLY a JSON object of the exact shape:\n" +
        "{\"summary\": string, \"picks\": [{\"ticker\": string, \"action\": string, \"allocation_pct\": number, \"reason\": string, \"risk_note\": string, \"fit\": string}]}\n" +
        "where action is one of BUY, SELL, HOLD and allocation_pct is a number 0-100. Keep reason and risk_note to one sentence each. " +
        "Include in the summary that this is informational only and not financial advice, and note that allocation_pct is the suggested split of the user's stock allocation.";

    public static string BuildUserPrompt(PortfolioResponse profile, IReadOnlyCollection<StockPrediction> predictions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("USER RISK PROFILE:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Risk profile: {profile.RiskProfile}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"- Target allocation: stocks {profile.StocksPercentage}%, bonds {profile.BondsPercentage}%, ETFs {profile.EtfsPercentage}%, cash {profile.CashPercentage}%");
        sb.AppendLine();
        sb.AppendLine("CANDIDATE STOCKS (today's risk-graded predictions, 30-day horizon):");
        foreach (StockPrediction p in predictions)
        {
            string pt = p.PtUpsidePct.HasValue
                ? p.PtUpsidePct.Value.ToString("F1", CultureInfo.InvariantCulture) + "%"
                : "n/a";
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"- {p.Ticker}: dir={p.Direction} change={p.ChangePct:F2}% conf={p.Confidence:F2} " +
                $"sentiment={p.SentimentScore:F2}({p.Signal}) agreement={p.Agreement} risk={p.RiskLevel} " +
                $"conviction={p.ConvictionScore:F2} ptUpside={pt} flags=[{string.Join(",", p.RiskFlags)}]");
        }
        sb.AppendLine();
        sb.AppendLine("Return the JSON recommendation object now.");
        return sb.ToString();
    }
}
