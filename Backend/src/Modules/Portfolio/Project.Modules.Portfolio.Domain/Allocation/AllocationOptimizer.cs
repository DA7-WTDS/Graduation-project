using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Strategies;

namespace Project.Modules.Portfolio.Domain.Allocation;

/// <summary>One equity candidate from the latest daily run, best first.
/// Rsi14/PctVsSma50 feed the tactical dip rule; SentimentSignal is the
/// "not deteriorating" check (POSITIVE/NEUTRAL/NEGATIVE).</summary>
public sealed record RankedEquity(
    string Symbol,
    double Score,
    string Direction,
    string RiskLevel,
    string SentimentSignal = "NEUTRAL",
    double? Rsi14 = null,
    double? PctVsSma50 = null);

public sealed record AllocationPosition(
    string Symbol,
    string Sleeve,
    double Weight,               // fraction of the whole portfolio
    decimal EstimatedValue,
    string Rationale);

public sealed record AllocationResult(
    IReadOnlyList<AllocationPosition> Positions,
    IReadOnlyList<string> Assumptions,
    string InputsHash);

public sealed record AllocationOptions(
    int TopN = 10,
    double MaxPositionWeight = 0.15,     // of the whole portfolio (single stocks only)
    double MaxSectorWeight = 0.30,       // of the whole portfolio (equities only)
    decimal MinPositionValue = 25m,
    double MinVolFloor = 0.05,           // avoids score/vol exploding on sleepy names
    // Tactical dip rule (§ 3.4): oversold AND quality AND sentiment-not-deteriorating.
    double TacticalRsiMax = 35,
    double TacticalDipMin = -0.05,       // at least 5% below the 50-DMA
    double TacticalMinAdv = 5_000_000,   // liquidity floor (quality proxy until fundamentals are licensed)
    int TacticalTopN = 5);

/// <summary>
/// The deterministic allocation optimizer (§ 3.3, D6): same inputs ⇒ same
/// portfolio, every time — no LLM anywhere near weights. Template buckets say
/// how much goes where; the registry says what qualifies; the ranking says
/// which equities. v1 sleeves: rules buckets (stability/cash/ETF-core) and the
/// ranked-equity core. Tactical and speculative buckets fold into core until
/// their engines land (§ 3.4) — recorded in Assumptions so nothing is silent.
/// </summary>
public static class AllocationOptimizer
{
    public static AllocationResult Build(
        IReadOnlyList<TemplateBucket> buckets,
        RiskProfile riskBand,
        IReadOnlyList<Instrument> instruments,
        IReadOnlyList<RankedEquity> rankings,
        decimal amount,
        AllocationOptions? options = null)
    {
        AllocationOptions opts = options ?? new AllocationOptions();
        var assumptions = new List<string>();
        var positions = new List<AllocationPosition>();

        List<Instrument> active = instruments.Where(i => i.IsActive).ToList();

        // Tactical candidates are computed up front: when the market offers no
        // qualifying dips today, the tactical bucket folds into core instead of
        // sitting empty. Speculative stays folded until the IPO/catalyst engine
        // (needs the § 1.6 event stream + a licensed IPO calendar).
        List<(RankedEquity Rank, Instrument Instrument)> dips = SelectTacticalCandidates(rankings, active, riskBand, opts);

        var effective = new List<TemplateBucket>();
        double foldedIntoCore = 0;
        foreach (TemplateBucket bucket in buckets)
        {
            if (bucket.Sleeve == Sleeves.Speculative)
            {
                foldedIntoCore += bucket.Weight;
                assumptions.Add($"{bucket.Sleeve} sleeve ({bucket.Weight:P0}) folded into core until the IPO/catalyst engine ships (§ 3.4).");
            }
            else if (bucket.Sleeve == Sleeves.Tactical && dips.Count == 0)
            {
                foldedIntoCore += bucket.Weight;
                assumptions.Add($"No qualifying dip opportunities today — the tactical sleeve ({bucket.Weight:P0}) folded into core.");
            }
            else
            {
                effective.Add(bucket);
            }
        }

        if (foldedIntoCore > 0)
        {
            int coreIdx = effective.FindIndex(b => b.Sleeve == Sleeves.Core);
            if (coreIdx >= 0)
            {
                effective[coreIdx] = effective[coreIdx] with { Weight = effective[coreIdx].Weight + foldedIntoCore };
            }
            else
            {
                effective.Add(new TemplateBucket(Sleeves.Core, foldedIntoCore, new BucketRules(Types: ["stock"])));
            }
        }

        // ---- build each bucket ----
        foreach (TemplateBucket bucket in effective)
        {
            bool isRankedCore = bucket.Sleeve == Sleeves.Core
                && (bucket.Rules.Types is null || bucket.Rules.Types.Contains("stock", StringComparer.OrdinalIgnoreCase));

            if (bucket.Sleeve == Sleeves.Tactical)
            {
                positions.AddRange(BuildTactical(bucket, dips, amount, opts, assumptions));
            }
            else if (isRankedCore)
            {
                // A name the tactical sleeve is buying must not be double-bought
                // by core (only relevant when a tactical bucket is actually live).
                bool tacticalActive = effective.Any(b => b.Sleeve == Sleeves.Tactical);
                IReadOnlyList<RankedEquity> coreRankings = tacticalActive
                    ? rankings.Where(r => !dips.Any(d => d.Rank.Symbol.Equals(r.Symbol, StringComparison.OrdinalIgnoreCase))).ToList()
                    : rankings;
                positions.AddRange(BuildCore(bucket, riskBand, active, coreRankings, amount, opts, assumptions));
            }
            else
            {
                positions.AddRange(BuildRulesBucket(bucket, active, amount, assumptions));
            }
        }

        // ---- renormalize (dropped weight from unfillable buckets/positions) ----
        double total = positions.Sum(p => p.Weight);
        if (total > 0 && Math.Abs(total - 1.0) > 1e-9)
        {
            if (total < 0.999)
            {
                assumptions.Add($"{1 - total:P1} of weight was unfillable and redistributed proportionally.");
            }

            positions = positions
                .Select(p => p with { Weight = p.Weight / total })
                .ToList();
        }

        // Renormalization can push a capped equity back over the limit, and a
        // thin core cohort can't always carry its bucket under the cap — either
        // way the surplus belongs in the defensive sleeves, not silently spread.
        // The cap targets single-stock concentration only; broad ETFs (e.g. the
        // retirement template's 40% index bucket) are exempt by design.
        var stockSymbols = active
            .Where(i => i.Type == InstrumentType.Stock)
            .Select(i => i.Symbol)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        positions = EnforceCoreCap(positions, opts.MaxPositionWeight, stockSymbols, assumptions);

        positions = positions
            .Select(p => p with { EstimatedValue = Math.Round(amount * (decimal)p.Weight, 2) })
            .ToList();

        return new AllocationResult(
            positions.OrderByDescending(p => p.Weight).ThenBy(p => p.Symbol, StringComparer.Ordinal).ToList(),
            assumptions,
            ComputeInputsHash(buckets, riskBand, active, rankings, amount, opts));
    }

    // ---- rules-driven buckets: stability, cash ladders, broad-market ETFs ----

    private static IEnumerable<AllocationPosition> BuildRulesBucket(
        TemplateBucket bucket,
        List<Instrument> instruments,
        decimal amount,
        List<string> assumptions)
    {
        List<Instrument> matches = instruments.Where(i => MatchesRules(i, bucket.Rules)).ToList();
        if (matches.Count == 0)
        {
            assumptions.Add($"No registry instrument matches the {bucket.Sleeve} bucket rules — its {bucket.Weight:P0} was redistributed.");
            yield break;
        }

        // One instrument per asset class (lowest vol wins, symbol breaks ties);
        // the bucket weight splits equally across the asset classes present.
        var picks = matches
            .GroupBy(i => i.AssetClass)
            .OrderBy(g => g.Key)
            .Select(g => g
                .OrderBy(i => i.RealizedVol1Y ?? double.MaxValue)
                .ThenBy(i => i.Symbol, StringComparer.Ordinal)
                .First())
            .ToList();

        double weightEach = bucket.Weight / picks.Count;
        foreach (Instrument pick in picks)
        {
            yield return new AllocationPosition(
                pick.Symbol,
                bucket.Sleeve,
                weightEach,
                Math.Round((decimal)weightEach * amount, 2),
                $"{pick.AssetClass} via {pick.Type}, vol {(pick.RealizedVol1Y is double v ? v.ToString("P0") : "n/a")}");
        }
    }

    private static bool MatchesRules(Instrument i, BucketRules rules)
    {
        bool assetOk = rules.AssetClasses is null
            || rules.AssetClasses.Contains(ToToken(i.AssetClass), StringComparer.OrdinalIgnoreCase);
        bool typeOk = rules.Types is null
            || rules.Types.Contains(ToToken(i.Type), StringComparer.OrdinalIgnoreCase);
        return assetOk && typeOk;
    }

    private static string ToToken(AssetClass a) => a switch
    {
        AssetClass.FixedIncome => "fixed_income",
        AssetClass.CashLike => "cash_like",
        _ => a.ToString().ToLowerInvariant()
    };

    private static string ToToken(InstrumentType t) => t switch
    {
        InstrumentType.MmFund => "mm_fund",
        _ => t.ToString().ToLowerInvariant()
    };

    // ---- the ranked-equity core sleeve ----

    private static List<AllocationPosition> BuildCore(
        TemplateBucket bucket,
        RiskProfile riskBand,
        List<Instrument> instruments,
        IReadOnlyList<RankedEquity> rankings,
        decimal amount,
        AllocationOptions opts,
        List<string> assumptions)
    {
        var bySymbol = instruments
            .Where(i => i.Type == InstrumentType.Stock)
            .ToDictionary(i => i.Symbol, StringComparer.OrdinalIgnoreCase);

        // Risk-grade filter: capacity-capped users never see HIGH-risk names.
        string[] allowed = AllowedRiskLevels(riskBand);

        List<(RankedEquity Rank, Instrument Instrument)> candidates = rankings
            .Where(r => r.Direction.Equals("UP", StringComparison.OrdinalIgnoreCase))
            .Where(r => allowed.Contains(r.RiskLevel, StringComparer.OrdinalIgnoreCase))
            .Where(r => bySymbol.ContainsKey(r.Symbol))
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Symbol, StringComparer.Ordinal)
            .Take(opts.TopN)
            .Select(r => (r, bySymbol[r.Symbol]))
            .ToList();

        if (candidates.Count == 0)
        {
            assumptions.Add($"No ranked equity passed the {riskBand} risk filter — the core bucket ({bucket.Weight:P0}) was redistributed.");
            return [];
        }

        // Inverse-vol tilt: weight ∝ score / vol. Missing vol → median of the
        // cohort; a floor keeps sleepy names from dominating.
        List<double> knownVols = candidates
            .Where(c => c.Instrument.RealizedVol1Y is not null)
            .Select(c => c.Instrument.RealizedVol1Y!.Value)
            .OrderBy(v => v)
            .ToList();
        double fallbackVol = knownVols.Count > 0 ? knownVols[knownVols.Count / 2] : 0.25;

        Dictionary<string, double> raw = candidates.ToDictionary(
            c => c.Rank.Symbol,
            c => Math.Max(c.Rank.Score, 1e-6) / Math.Max(c.Instrument.RealizedVol1Y ?? fallbackVol, opts.MinVolFloor),
            StringComparer.OrdinalIgnoreCase);

        Dictionary<string, double> weights = Normalize(raw, bucket.Weight);

        // Portfolio-level position cap, then sector cap, then the position cap
        // again (sector redistribution can push someone over). Water-filling is
        // deterministic and bounded.
        weights = CapPositions(weights, opts.MaxPositionWeight);
        weights = CapSectors(weights, candidates.ToDictionary(c => c.Rank.Symbol, c => c.Instrument.Sector, StringComparer.OrdinalIgnoreCase), opts.MaxSectorWeight, assumptions);
        weights = CapPositions(weights, opts.MaxPositionWeight);

        // Min position size: dust positions get dropped and the bucket renormalized once.
        List<string> dust = weights.Where(w => (decimal)w.Value * amount < opts.MinPositionValue).Select(w => w.Key).ToList();
        if (dust.Count > 0 && dust.Count < weights.Count)
        {
            assumptions.Add($"{dust.Count} core position(s) below {opts.MinPositionValue:C0} dropped for the invested amount.");
            foreach (string s in dust)
            {
                weights.Remove(s);
            }

            weights = Normalize(weights, bucket.Weight);
            weights = CapPositions(weights, opts.MaxPositionWeight);
        }

        var rankIndex = candidates.Select((c, i) => (c.Rank.Symbol, Index: i + 1))
            .ToDictionary(x => x.Symbol, x => x.Index, StringComparer.OrdinalIgnoreCase);

        return weights
            .OrderByDescending(w => w.Value)
            .ThenBy(w => w.Key, StringComparer.Ordinal)
            .Select(w => new AllocationPosition(
                w.Key,
                Sleeves.Core,
                w.Value,
                Math.Round((decimal)w.Value * amount, 2),
                $"rank #{rankIndex[w.Key]}, vol {(bySymbol[w.Key].RealizedVol1Y is double v ? v.ToString("P0") : "n/a")}, {bySymbol[w.Key].Sector ?? "sector n/a"}"))
            .ToList();
    }

    // ---- the tactical dip-buyer sleeve (§ 3.4) ----

    /// <summary>Oversold AND quality AND sentiment-not-deteriorating, all from
    /// data (rankings + registry), best dips first. Quality is a liquidity
    /// proxy until fundamentals are licensed.</summary>
    private static List<(RankedEquity Rank, Instrument Instrument)> SelectTacticalCandidates(
        IReadOnlyList<RankedEquity> rankings,
        List<Instrument> instruments,
        RiskProfile riskBand,
        AllocationOptions opts)
    {
        var bySymbol = instruments
            .Where(i => i.Type == InstrumentType.Stock)
            .ToDictionary(i => i.Symbol, StringComparer.OrdinalIgnoreCase);

        string[] allowed = AllowedRiskLevels(riskBand);

        return rankings
            .Where(r => r.Rsi14 is double rsi && rsi < opts.TacticalRsiMax)
            .Where(r => r.PctVsSma50 is double dip && dip <= opts.TacticalDipMin)
            .Where(r => !r.SentimentSignal.Equals("NEGATIVE", StringComparison.OrdinalIgnoreCase))
            .Where(r => allowed.Contains(r.RiskLevel, StringComparer.OrdinalIgnoreCase))
            .Where(r => bySymbol.TryGetValue(r.Symbol, out Instrument? i)
                && (i.AvgDailyValueTraded ?? 0) >= opts.TacticalMinAdv)
            .OrderBy(r => r.Rsi14) // deepest oversold first
            .ThenBy(r => r.Symbol, StringComparer.Ordinal)
            .Take(opts.TacticalTopN)
            .Select(r => (r, bySymbol[r.Symbol]))
            .ToList();
    }

    private static List<AllocationPosition> BuildTactical(
        TemplateBucket bucket,
        List<(RankedEquity Rank, Instrument Instrument)> dips,
        decimal amount,
        AllocationOptions opts,
        List<string> assumptions)
    {
        // Weight ∝ dip depth (how far below the RSI threshold) over volatility —
        // deeper, calmer dips get more of the bounded sleeve.
        Dictionary<string, double> raw = dips.ToDictionary(
            d => d.Rank.Symbol,
            d => Math.Max(opts.TacticalRsiMax - d.Rank.Rsi14!.Value, 1.0)
                 / Math.Max(d.Instrument.RealizedVol1Y ?? 0.25, opts.MinVolFloor),
            StringComparer.OrdinalIgnoreCase);

        Dictionary<string, double> weights = CapPositions(Normalize(raw, bucket.Weight), opts.MaxPositionWeight);

        List<string> dust = weights.Where(w => (decimal)w.Value * amount < opts.MinPositionValue).Select(w => w.Key).ToList();
        if (dust.Count > 0 && dust.Count < weights.Count)
        {
            foreach (string s in dust)
            {
                weights.Remove(s);
            }

            weights = CapPositions(Normalize(weights, bucket.Weight), opts.MaxPositionWeight);
        }

        var bySymbol = dips.ToDictionary(d => d.Rank.Symbol, StringComparer.OrdinalIgnoreCase);
        return weights
            .OrderByDescending(w => w.Value)
            .ThenBy(w => w.Key, StringComparer.Ordinal)
            .Select(w => new AllocationPosition(
                w.Key,
                Sleeves.Tactical,
                w.Value,
                Math.Round((decimal)w.Value * amount, 2),
                $"dip: RSI {bySymbol[w.Key].Rank.Rsi14!.Value:F0}, {bySymbol[w.Key].Rank.PctVsSma50:P1} vs 50-DMA, {bySymbol[w.Key].Instrument.Sector ?? "sector n/a"}"))
            .ToList();
    }

    private static string[] AllowedRiskLevels(RiskProfile riskBand) => riskBand switch
    {
        RiskProfile.Conservative => ["LOW"],
        RiskProfile.Moderate => ["LOW", "MEDIUM"],
        _ => ["LOW", "MEDIUM", "HIGH"]
    };

    /// <summary>Portfolio-level position cap for single stocks (core + tactical):
    /// capped names give their surplus to everything else (stability first by
    /// construction, since it is the bulk of the uncapped weight). Water-fills
    /// to a fixpoint.</summary>
    private static List<AllocationPosition> EnforceCoreCap(
        List<AllocationPosition> positions, double cap, HashSet<string> stockSymbols, List<string> assumptions)
    {
        var weights = positions.ToDictionary(p => $"{p.Sleeve}:{p.Symbol}", p => p.Weight, StringComparer.Ordinal);
        bool IsCore(string key) =>
            (key.StartsWith($"{Sleeves.Core}:", StringComparison.Ordinal)
                && stockSymbols.Contains(key[(Sleeves.Core.Length + 1)..]))
            || (key.StartsWith($"{Sleeves.Tactical}:", StringComparison.Ordinal)
                && stockSymbols.Contains(key[(Sleeves.Tactical.Length + 1)..]));
        bool disclosed = false;

        for (int pass = 0; pass < positions.Count + 1; pass++)
        {
            List<string> over = weights.Where(w => IsCore(w.Key) && w.Value > cap + 1e-12).Select(w => w.Key).ToList();
            if (over.Count == 0)
            {
                break;
            }

            double excess = over.Sum(k => weights[k] - cap);
            foreach (string k in over)
            {
                weights[k] = cap;
            }

            List<string> absorbers = weights.Keys.Where(k => !IsCore(k) || weights[k] < cap - 1e-12).ToList();
            if (absorbers.Count == 0)
            {
                assumptions.Add($"Position cap {cap:P0} could not be fully enforced — no defensive positions available to absorb the surplus.");
                foreach (string k in over)
                {
                    weights[k] += excess / over.Count;
                }

                break;
            }

            if (!disclosed)
            {
                assumptions.Add($"Core surplus above the {cap:P0} position cap was shifted to defensive sleeves.");
                disclosed = true;
            }

            double absorberSum = absorbers.Sum(k => weights[k]);
            foreach (string k in absorbers)
            {
                weights[k] += absorberSum > 0 ? excess * (weights[k] / absorberSum) : excess / absorbers.Count;
            }
        }

        return positions.Select(p => p with { Weight = weights[$"{p.Sleeve}:{p.Symbol}"] }).ToList();
    }

    private static Dictionary<string, double> Normalize(Dictionary<string, double> raw, double targetSum)
    {
        double sum = raw.Values.Sum();
        return sum <= 0
            ? raw
            : raw.ToDictionary(kv => kv.Key, kv => kv.Value / sum * targetSum, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, double> CapPositions(Dictionary<string, double> weights, double cap)
    {
        // Iterative water-filling: cap the over-limit names, hand the excess to
        // the uncapped ones proportionally, repeat until stable.
        var result = new Dictionary<string, double>(weights, StringComparer.OrdinalIgnoreCase);
        for (int pass = 0; pass < result.Count; pass++)
        {
            List<string> over = result.Where(w => w.Value > cap + 1e-12).Select(w => w.Key).ToList();
            if (over.Count == 0)
            {
                break;
            }

            double excess = over.Sum(s => result[s] - cap);
            foreach (string s in over)
            {
                result[s] = cap;
            }

            List<string> under = result.Where(w => w.Value < cap - 1e-12).Select(w => w.Key).ToList();
            if (under.Count == 0)
            {
                break; // everyone at cap — total shrinks; Build() renormalizes across buckets
            }

            double underSum = under.Sum(s => result[s]);
            foreach (string s in under)
            {
                result[s] += underSum > 0 ? excess * (result[s] / underSum) : excess / under.Count;
            }
        }

        return result;
    }

    private static Dictionary<string, double> CapSectors(
        Dictionary<string, double> weights,
        Dictionary<string, string?> sectors,
        double cap,
        List<string> assumptions)
    {
        var result = new Dictionary<string, double>(weights, StringComparer.OrdinalIgnoreCase);
        var overweight = result
            .GroupBy(w => sectors.GetValueOrDefault(w.Key) ?? "Unknown")
            .Where(g => g.Sum(w => w.Value) > cap + 1e-12)
            .ToList();

        foreach (var sector in overweight)
        {
            double sectorSum = sector.Sum(w => w.Value);
            double scale = cap / sectorSum;
            double excess = sectorSum - cap;
            foreach (var w in sector)
            {
                result[w.Key] = w.Value * scale;
            }

            List<string> others = result.Keys.Where(s => (sectors.GetValueOrDefault(s) ?? "Unknown") != sector.Key).ToList();
            double otherSum = others.Sum(s => result[s]);
            foreach (string s in others)
            {
                result[s] += otherSum > 0 ? excess * (result[s] / otherSum) : excess / Math.Max(others.Count, 1);
            }

            assumptions.Add($"{sector.Key} sector capped at {cap:P0} in the core sleeve.");
        }

        return result;
    }

    private static string ComputeInputsHash(
        IReadOnlyList<TemplateBucket> buckets,
        RiskProfile riskBand,
        List<Instrument> instruments,
        IReadOnlyList<RankedEquity> rankings,
        decimal amount,
        AllocationOptions opts)
    {
        var canonical = new
        {
            buckets,
            riskBand = riskBand.ToString(),
            instruments = instruments
                .OrderBy(i => i.Symbol, StringComparer.Ordinal)
                .Select(i => new { i.Symbol, i.Type, i.AssetClass, i.RealizedVol1Y, i.Sector, i.IsActive }),
            rankings = rankings.Select(r => new { r.Symbol, r.Score, r.Direction, r.RiskLevel }),
            amount,
            opts,
        };
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical)));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
