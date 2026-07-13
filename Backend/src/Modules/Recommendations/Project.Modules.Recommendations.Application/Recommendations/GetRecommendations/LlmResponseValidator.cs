using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Domain.Holdings;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

/// <summary>
/// Hard verification of the LLM's output against the context pack (§ 3.6, D6).
/// The prompt asks Gemini to behave; this class checks that it did. Any
/// violation is treated exactly like malformed JSON: regenerate, then fail
/// closed. Nothing unverified ever reaches a user.
/// </summary>
internal static class LlmResponseValidator
{
    private const double AllocationSumTolerance = 2.0; // percentage points
    private static readonly string[] ValidActions = ["BUY", "SELL", "HOLD"];
    private static readonly string[] ConservativeForbiddenFlags = ["signal_contradiction", "internal_conflict"];

    public static IReadOnlyList<string> Validate(
        LlmRecommendationResult result,
        IReadOnlyCollection<StockPrediction> candidates,
        IReadOnlyCollection<UserHolding> holdings,
        string riskProfile)
    {
        var violations = new List<string>();

        var candidatesByTicker = candidates.ToDictionary(p => p.Ticker, StringComparer.OrdinalIgnoreCase);
        var heldTickers = holdings.Select(h => h.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool conservative = string.Equals(riskProfile, "Conservative", StringComparison.OrdinalIgnoreCase);

        if (result.Picks.Count == 0)
        {
            violations.Add("no picks returned");
            return violations;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        double allocationSum = 0;

        foreach (RecommendationItem pick in result.Picks)
        {
            string t = pick.Ticker;

            if (!seen.Add(t))
            {
                violations.Add($"{t}: duplicated pick");
            }

            if (!ValidActions.Contains(pick.Action, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add($"{t}: invalid action '{pick.Action}'");
                continue;
            }

            bool isCandidate = candidatesByTicker.TryGetValue(t, out StockPrediction? candidate);
            bool isHeld = heldTickers.Contains(t);

            // Universe rule: every ticker must come from the context pack.
            if (!isCandidate && !isHeld)
            {
                violations.Add($"{t}: not in today's candidates or current holdings (invented ticker)");
                continue;
            }

            // A BUY needs a fresh signal; held names without one may only be HOLD/SELL.
            if (!isCandidate && string.Equals(pick.Action, "BUY", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{t}: BUY without a fresh signal (held name absent from today's run)");
            }

            if (pick.AllocationPct is < 0 or > 100)
            {
                violations.Add($"{t}: allocation {pick.AllocationPct} outside 0-100");
            }

            if (string.Equals(pick.Action, "SELL", StringComparison.OrdinalIgnoreCase))
            {
                if (pick.AllocationPct != 0)
                {
                    violations.Add($"{t}: SELL must carry allocation 0, got {pick.AllocationPct}");
                }
            }
            else
            {
                allocationSum += pick.AllocationPct;
            }

            // Risk grading is a hard rule, not a suggestion (D6): capacity-capped
            // users never receive HIGH-risk or conflicted BUYs.
            if (conservative
                && string.Equals(pick.Action, "BUY", StringComparison.OrdinalIgnoreCase)
                && candidate is not null)
            {
                if (string.Equals(candidate.RiskLevel, "HIGH", StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{t}: HIGH-risk BUY for a Conservative profile");
                }

                if (candidate.RiskFlags.Any(f => ConservativeForbiddenFlags.Contains(f, StringComparer.OrdinalIgnoreCase)))
                {
                    violations.Add($"{t}: flagged ({string.Join(",", candidate.RiskFlags)}) BUY for a Conservative profile");
                }
            }
        }

        if (Math.Abs(allocationSum - 100) > AllocationSumTolerance)
        {
            violations.Add($"BUY/HOLD allocations sum to {allocationSum:F1}, expected 100 ±{AllocationSumTolerance}");
        }

        return violations;
    }
}
