using System.Text.Json;
using Project.Common.Domain.Abstractions;
using Project.Modules.Portfolio.Domain.Allocation;
using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Domain.Proposals;

public enum ProposalStatus
{
    /// <summary>Generated and offered to the user, not yet acted on.</summary>
    Proposed,

    /// <summary>The user accepted this proposal — it is their current target.</summary>
    Accepted,

    /// <summary>A later accepted proposal replaced this one.</summary>
    Superseded
}

/// <summary>
/// An immutable, versioned snapshot of a portfolio the optimizer proposed for a
/// goal (Phase 4). The allocation is frozen at creation — accepting only flips
/// status, never the numbers, so the record is a faithful audit trail of what
/// was offered and when. The InputsHash ties it back to the exact registry +
/// ranking state that produced it (§ 3.3, D6).
/// </summary>
public sealed class PortfolioProposal : Entity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private PortfolioProposal() { }

    public Guid Id { get; private set; }
    public Guid GoalId { get; private set; }
    public int Version { get; private set; }

    public string TemplateKey { get; private set; }
    public string TemplateName { get; private set; }
    public string RebalanceCadence { get; private set; }
    public double DrawdownAlertPct { get; private set; }
    public RiskProfile RiskBand { get; private set; }
    public int EffectiveRisk { get; private set; }
    public decimal Amount { get; private set; }

    public string PositionsJson { get; private set; }
    public string AssumptionsJson { get; private set; }
    public string InputsHash { get; private set; }

    public ProposalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    public IReadOnlyList<AllocationPosition> GetPositions() =>
        JsonSerializer.Deserialize<List<AllocationPosition>>(PositionsJson, JsonOptions) ?? [];

    public IReadOnlyList<string> GetAssumptions() =>
        JsonSerializer.Deserialize<List<string>>(AssumptionsJson, JsonOptions) ?? [];

    public static PortfolioProposal Create(
        Guid goalId,
        int version,
        string templateKey,
        string templateName,
        string rebalanceCadence,
        double drawdownAlertPct,
        RiskProfile riskBand,
        int effectiveRisk,
        decimal amount,
        IReadOnlyList<AllocationPosition> positions,
        IReadOnlyList<string> assumptions,
        string inputsHash)
    {
        return new PortfolioProposal
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            Version = version,
            TemplateKey = templateKey,
            TemplateName = templateName,
            RebalanceCadence = rebalanceCadence,
            DrawdownAlertPct = drawdownAlertPct,
            RiskBand = riskBand,
            EffectiveRisk = effectiveRisk,
            Amount = amount,
            PositionsJson = JsonSerializer.Serialize(positions, JsonOptions),
            AssumptionsJson = JsonSerializer.Serialize(assumptions, JsonOptions),
            InputsHash = inputsHash,
            Status = ProposalStatus.Proposed,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Idempotent: accepting an already-accepted proposal is a no-op.
    /// A superseded proposal can never be re-accepted.</summary>
    public bool Accept()
    {
        if (Status == ProposalStatus.Accepted)
        {
            return false;
        }

        if (Status == ProposalStatus.Superseded)
        {
            throw new InvalidOperationException("A superseded proposal cannot be accepted.");
        }

        Status = ProposalStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>Marks a previously accepted proposal as replaced by a newer one.</summary>
    public void Supersede()
    {
        if (Status == ProposalStatus.Accepted)
        {
            Status = ProposalStatus.Superseded;
        }
    }
}
