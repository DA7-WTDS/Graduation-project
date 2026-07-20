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

    [Fact]
    public void Create_Published_Should_AlsoRaisePublishedEvent()
    {
        var run = DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], DailyRunStatus.Published);

        run.Status.Should().Be(DailyRunStatus.Published);
        run.DomainEvents.Should().ContainSingle(e => e is DailyRunPublishedDomainEvent);
    }

    [Theory]
    [InlineData(DailyRunStatus.PendingReview)]
    [InlineData(DailyRunStatus.Quarantined)]
    public void Create_NonPublished_Should_NotRaisePublishedEvent(DailyRunStatus status)
    {
        var run = DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], status, "gate failure detail");

        run.Status.Should().Be(status);
        run.StatusReason.Should().Be("gate failure detail");
        run.DomainEvents.Should().NotContain(e => e is DailyRunPublishedDomainEvent);
    }

    [Fact]
    public void Create_Should_Throw_WhenLandingAsRolledBack()
    {
        var act = () => DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], DailyRunStatus.RolledBack);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(DailyRunStatus.PendingReview, DailyRunStatus.Published)]
    [InlineData(DailyRunStatus.PendingReview, DailyRunStatus.Quarantined)]
    [InlineData(DailyRunStatus.Quarantined, DailyRunStatus.Published)]
    [InlineData(DailyRunStatus.Published, DailyRunStatus.RolledBack)]
    public void ChangeStatus_Should_AllowValidTransitions(DailyRunStatus from, DailyRunStatus to)
    {
        var run = DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], from);

        var result = run.ChangeStatus(to, "operator action");

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(to);
        run.StatusReason.Should().Be("operator action");
    }

    [Fact]
    public void ChangeStatus_Should_AllowRollbackUndo()
    {
        var run = DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], DailyRunStatus.Published);
        run.ChangeStatus(DailyRunStatus.RolledBack).IsSuccess.Should().BeTrue();

        var result = run.ChangeStatus(DailyRunStatus.Published, "false alarm");

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(DailyRunStatus.Published);
    }

    [Theory]
    [InlineData(DailyRunStatus.Published, DailyRunStatus.PendingReview)]
    [InlineData(DailyRunStatus.Published, DailyRunStatus.Quarantined)]
    [InlineData(DailyRunStatus.Quarantined, DailyRunStatus.RolledBack)]
    [InlineData(DailyRunStatus.PendingReview, DailyRunStatus.RolledBack)]
    public void ChangeStatus_Should_RejectInvalidTransitions(DailyRunStatus from, DailyRunStatus to)
    {
        var run = DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], from);

        var result = run.ChangeStatus(to);

        result.IsFailed.Should().BeTrue();
        run.Status.Should().Be(from);
    }

    [Fact]
    public void ChangeStatus_ToPublished_Should_RaisePublishedEvent()
    {
        var run = DailyRun.Create(DateTime.UtcNow, [Prediction("AAPL")], DailyRunStatus.PendingReview);
        run.DomainEvents.Should().NotContain(e => e is DailyRunPublishedDomainEvent);

        run.ChangeStatus(DailyRunStatus.Published, "approved");

        run.DomainEvents.Should().ContainSingle(e => e is DailyRunPublishedDomainEvent);
    }
}
