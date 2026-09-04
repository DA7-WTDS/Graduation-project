using System.Globalization;
using System.Text;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Domain.Holdings;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

/// <summary>Realized-outcome figures fed to the LLM (§ 3.6): the ONLY
/// performance numbers it is allowed to repeat.</summary>
public sealed record TrackRecordSnippet(double HitRate90D, int SampleSize);

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
        "4. Recommend the best 5-8 stocks for the user to BUY or HOLD (briefly justify each from the provided fields), plus a SELL for any current holding that no longer fits.\n" +
        "5. The user message may include CURRENT HOLDINGS — stocks the user already owns, each with its current % of their stock allocation. Treat these as the existing portfolio:\n" +
        "   - For each held ticker, recommend HOLD if today's data still supports keeping it, or SELL if it no longer fits (HIGH risk for a Conservative user, signal_contradiction/internal_conflict, weak conviction, or a DOWN prediction). If a held ticker is NOT in today's candidate list, default to HOLD (no fresh signal).\n" +
        "   - Recommend BUY for compelling candidate stocks the user does NOT already hold.\n" +
        "   - Prefer evolving the portfolio over churning it: keep good positions and replace only weak ones. If CURRENT HOLDINGS is 'none', recommend BUYs only.\n" +
        "6. allocation_pct is the TARGET share of the user's STOCK allocation after this rebalance. SELL picks MUST use allocation_pct 0. The allocation_pct of all BUY and HOLD picks MUST sum to 100. Weight by conviction and fit; diversify more evenly for Conservative profiles, allow more concentration for Aggressive.\n" +
        "7. Output ONLY a JSON object of the exact shape:\n" +
        "{\"summary\": string, \"picks\": [{\"ticker\": string, \"action\": string, \"allocation_pct\": number, \"reason\": string, \"risk_note\": string, \"fit\": string}]}\n" +
        "where action is one of BUY, SELL, HOLD and allocation_pct is a number 0-100. Keep reason and risk_note to one sentence each. " +
        "Include in the summary that this is informational only and not financial advice, and note that allocation_pct is the suggested split of the user's stock allocation.\n" +
        "8. If the user message includes OUR REAL TRACK RECORD, you may cite those exact figures in the summary — never any other performance number.\n" +
        "9. Write summary, reason, risk_note and fit in the language given under LANGUAGE (English or Arabic). " +
        "For Arabic use clear Modern Standard Arabic with a calm, non-promotional tone; keep tickers, JSON keys and the action values in English.\n" +
        "10. The candidate score (`rel` or `chg`) is a model signal over a ~30-day horizon, not a promise. Never state or imply a target price, a monetary gain, or a guaranteed return, and never convert the score into an amount of money. When the score is labelled `rel`, describe it as expected out- or under-performance versus the market, never as a price move.";

    public static string BuildUserPrompt(
        MonitoringProfileResponse profile,
        IReadOnlyCollection<StockPrediction> predictions,
        IReadOnlyCollection<UserHolding> holdings,
        TrackRecordSnippet? trackRecord = null,
        string language = "en")
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"LANGUAGE: {(string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase) ? "Arabic" : "English")}");
        sb.AppendLine();
        sb.AppendLine("USER RISK PROFILE:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Risk profile: {profile.RiskProfile}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Investment goal: {profile.GoalType}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Engagement preference: {profile.Engagement}");
        sb.AppendLine();

        if (trackRecord is not null && trackRecord.SampleSize > 0)
        {
            sb.AppendLine("OUR REAL TRACK RECORD (realized outcomes, honest figures — the only performance numbers you may cite):");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"- Last 90 days: direction hit rate {trackRecord.HitRate90D:P1} across {trackRecord.SampleSize} scored predictions.");
            sb.AppendLine();
        }

        if (holdings.Count == 0)
        {
            sb.AppendLine("CURRENT HOLDINGS: none (this is the user's first allocation — recommend BUYs only).");
        }
        else
        {
            sb.AppendLine("CURRENT HOLDINGS (stocks the user already owns; % is their current share of the stock allocation):");
            foreach (UserHolding h in holdings)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"- {h.Ticker}: {h.AllocationPct:F0}%");
            }
        }
        sb.AppendLine();

        // Which scale ChangePct is on depends on the serving stack, so the wording is
        // derived from the run itself — never hard-coded. Calling a relative score an
        // expected price move would put a forecast in the model's mouth, and it would
        // repeat that framing straight back to the user.
        bool relative = PredictionScale.IsRelative(predictions);

        sb.AppendLine(relative
            ? "CANDIDATE STOCKS (today's risk-graded signals, ~30-day horizon). `rel` is the model's RELATIVE-STRENGTH score: expected return versus the median stock in the universe, in percentage points. It is NOT a forecast of the share price."
            : "CANDIDATE STOCKS (today's risk-graded predictions, ~30-day horizon). `chg` is the model's expected 30-day return in percent.");
        foreach (StockPrediction p in predictions)
        {
            string pt = p.PtUpsidePct.HasValue
                ? p.PtUpsidePct.Value.ToString("F1", CultureInfo.InvariantCulture) + "%"
                : "n/a";
            string score = relative
                ? $"rel={p.ChangePct:+0.00;-0.00}pp"
                : $"chg={p.ChangePct:F2}%";
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"- {p.Ticker}: dir={p.Direction} {score} conf={p.Confidence:F2} " +
                $"sentiment={p.SentimentScore:F2}({p.Signal}) agreement={p.Agreement} risk={p.RiskLevel} " +
                $"conviction={p.ConvictionScore:F2} ptUpside={pt} flags=[{string.Join(",", p.RiskFlags)}]");
        }
        sb.AppendLine();
        sb.AppendLine("Return the JSON recommendation object now.");
        return sb.ToString();
    }
}
