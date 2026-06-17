using FluentAssertions;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.DailyRuns;

public class StockPredictionTests
{
    [Fact]
    public void Create_Should_SetAllFields()
    {
        var p = StockPrediction.Create(
            "AAPL", "UP", 12.5d, 0.91d, 0.4d, "POSITIVE", 4.2d, "Buy", 8.0d, 0.3d,
            "CONFIRMED", "MEDIUM", 0.85d, new[] { "extreme_move" }, "looks good");

        p.Id.Should().NotBeEmpty();
        p.Ticker.Should().Be("AAPL");
        p.Direction.Should().Be("UP");
        p.ChangePct.Should().Be(12.5d);
        p.Confidence.Should().Be(0.91d);
        p.Signal.Should().Be("POSITIVE");
        p.AnalystRating.Should().Be(4.2d);
        p.RiskLevel.Should().Be("MEDIUM");
        p.ConvictionScore.Should().Be(0.85d);
        p.RiskFlags.Should().ContainSingle().Which.Should().Be("extreme_move");
    }

    [Fact]
    public void Create_Should_DefaultNullRiskFlagsToEmpty()
    {
        var p = StockPrediction.Create(
            "AAPL", "UP", 1d, 0.5d, 0d, "NEUTRAL", null, null, null, null,
            "NEUTRAL", "LOW", 0.5d, null!, "r");

        p.RiskFlags.Should().NotBeNull();
        p.RiskFlags.Should().BeEmpty();
    }
}
