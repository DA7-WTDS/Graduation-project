using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.Holdings;

/// <summary>
/// A stock the user is treated as currently holding — derived from the picks of
/// the last recommendation run generated for them. Fed back into the next run's
/// prompt so the assistant can recommend SELL/HOLD on existing positions instead
/// of only ever recommending fresh BUYs.
/// </summary>
public sealed class UserHolding : Entity
{
    private UserHolding() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Ticker { get; private set; }
    public double AllocationPct { get; private set; }

    /// <summary>GeneratedAt of the run whose recommendation produced this holding.</summary>
    public DateTime RunGeneratedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static UserHolding Create(Guid userId, string ticker, double allocationPct, DateTime runGeneratedAt)
    {
        return new UserHolding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Ticker = ticker,
            AllocationPct = allocationPct,
            RunGeneratedAt = runGeneratedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
