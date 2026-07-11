using System.Text.Json;
using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Strategies;

/// <summary>Rules for one bucket of a template. All filters are registry
/// attributes (§ 3.1) — matching instruments are found at optimization time,
/// never named in the template.</summary>
public sealed record BucketRules(
    List<string>? AssetClasses = null,   // "equity" | "gold" | "fixed_income" | "cash_like"
    List<string>? Types = null);         // "stock" | "etf" | "fund" | "mm_fund"

/// <summary>One sleeve of a template: what fraction of the portfolio it gets
/// and which instruments qualify.</summary>
public sealed record TemplateBucket(string Sleeve, double Weight, BucketRules Rules);

/// <summary>
/// A strategy template (§ 3.2): the bridge from investor profile to portfolio
/// shape. Buckets are data (jsonb), so tuning weights or adding a sleeve is a
/// seed change, not a code change. Selection: goal type + effective risk +
/// speculative gate; most specific eligible template wins.
/// </summary>
public sealed class StrategyTemplate : Entity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private StrategyTemplate() { }

    public Guid Id { get; private set; }
    public string Key { get; private set; }              // stable slug, e.g. "balanced_growth"
    public string Name { get; private set; }
    public List<string> GoalTypes { get; private set; } = [];
    public int RiskMin { get; private set; }
    public int RiskMax { get; private set; }
    public bool RequiresSpeculativeUnlock { get; private set; }
    public string BucketsJson { get; private set; }
    public string RebalanceCadence { get; private set; } // semi_annual | monthly | weekly
    public double DrawdownAlertPct { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static StrategyTemplate Create(
        string key,
        string name,
        IEnumerable<string> goalTypes,
        int riskMin,
        int riskMax,
        bool requiresSpeculativeUnlock,
        IEnumerable<TemplateBucket> buckets,
        string rebalanceCadence,
        double drawdownAlertPct)
    {
        return new StrategyTemplate
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            GoalTypes = goalTypes.ToList(),
            RiskMin = riskMin,
            RiskMax = riskMax,
            RequiresSpeculativeUnlock = requiresSpeculativeUnlock,
            BucketsJson = SerializeBuckets(buckets),
            RebalanceCadence = rebalanceCadence,
            DrawdownAlertPct = drawdownAlertPct,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static string SerializeBuckets(IEnumerable<TemplateBucket> buckets) =>
        JsonSerializer.Serialize(buckets, JsonOptions);

    public IReadOnlyList<TemplateBucket> GetBuckets() =>
        JsonSerializer.Deserialize<List<TemplateBucket>>(BucketsJson, JsonOptions) ?? [];

    public bool Matches(string goalType, int effectiveRisk, bool speculativeUnlocked) =>
        IsActive
        && GoalTypes.Contains(goalType, StringComparer.OrdinalIgnoreCase)
        && effectiveRisk >= RiskMin
        && effectiveRisk <= RiskMax
        && (!RequiresSpeculativeUnlock || speculativeUnlocked);
}

/// <summary>Deterministic template selection: among eligible templates the most
/// specific wins — a gated template beats an open one, then the tightest
/// (highest) risk floor. Ties break on Key for full determinism.</summary>
public static class TemplateSelector
{
    public static StrategyTemplate? Select(
        IEnumerable<StrategyTemplate> templates,
        string goalType,
        int effectiveRisk,
        bool speculativeUnlocked)
    {
        return templates
            .Where(t => t.Matches(goalType, effectiveRisk, speculativeUnlocked))
            .OrderByDescending(t => t.RequiresSpeculativeUnlock)
            .ThenByDescending(t => t.RiskMin)
            .ThenBy(t => t.Key, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
