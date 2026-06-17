using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.DailyRuns;

public class DailyRunTests
{
    private static StockPrediction Prediction(string ticker) =>
        StockPrediction.Create(ticker, "UP", 5d, 0.8d, 0.5d, "POSITIVE", null, null, null, null,
            "CONFIRMED", "LOW", 0.9d, new[] { "none" }, "rationale");

    [Fact]
    public void Create_Should_SetGeneratedAt_Count_AndPredictions()
    {
        var generatedAt = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);
        var predictions = new List<StockPrediction> { Prediction("AAPL"), Prediction("MSFT") };

        var run = DailyRun.Create(generatedAt, predictions);

        run.Id.Should().NotBeEmpty();
        run.GeneratedAt.Should().Be(generatedAt);
        run.Count.Should().Be(2);
        run.Predictions.Should().HaveCount(2);
        run.Predictions.Select(p => p.Ticker).Should().Contain(new[] { "AAPL", "MSFT" });
    }

    [Fact]
    public void Create_Should_RaiseDailyRunIngestedDomainEvent()
    {
        var run = DailyRun.Create(DateTime.UtcNow, new List<StockPrediction> { Prediction("AAPL") });

        run.DomainEvents.Should().ContainSingle(e => e is DailyRunIngestedDomainEvent);
    }
}
