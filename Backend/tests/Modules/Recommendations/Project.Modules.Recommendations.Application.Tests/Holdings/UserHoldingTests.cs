using FluentAssertions;
using Project.Modules.Recommendations.Domain.Holdings;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.Holdings;

public class UserHoldingTests
{
    [Fact]
    public void Create_Should_SetAllFields()
    {
        var userId = Guid.NewGuid();
        var runAt = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);

        var holding = UserHolding.Create(userId, "AAPL", 13.5d, runAt);

        holding.Id.Should().NotBeEmpty();
        holding.UserId.Should().Be(userId);
        holding.Ticker.Should().Be("AAPL");
        holding.AllocationPct.Should().Be(13.5d);
        holding.RunGeneratedAt.Should().Be(runAt);
        holding.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}
