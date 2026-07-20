using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Abstractions.Pipeline;
using Project.Modules.Recommendations.Application.DailyRuns.Reproduce;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.DailyRuns.Reproduce;

public class ReproducePredictionQueryHandlerTests
{
    private const string Snapshot = """{"v":1,"lstm_window":[[0.1,0.2,0.3,0.4,0.5]],"tech_last":[0.1]}""";

    private readonly IDailyRunRepository _repository = Substitute.For<IDailyRunRepository>();
    private readonly IPipelineReproducer _reproducer = Substitute.For<IPipelineReproducer>();
    private readonly ReproducePredictionQueryHandler _handler;

    public ReproducePredictionQueryHandlerTests()
    {
        _handler = new ReproducePredictionQueryHandler(_repository, _reproducer);
    }

    private static StockPrediction Prediction(
        string? featuresJson = Snapshot,
        string? modelVersion = "modelaaa",
        string? scalerHash = "scalerbbb",
        double changePct = 5d,
        double confidence = 0.8d,
        string direction = "UP") =>
        StockPrediction.Create("AAPL", direction, changePct, confidence, 0.5d, "POSITIVE",
            null, null, null, null, "CONFIRMED", "LOW", 0.9d, ["none"], "rationale",
            null, null, featuresJson, modelVersion, scalerHash);

    private void Arrange(StockPrediction prediction) =>
        _repository.GetPredictionForAuditAsync(prediction.Id, Arg.Any<CancellationToken>())
            .Returns(new PredictionAudit(prediction, DateTime.UtcNow, "Published"));

    private void Recomputes(string direction = "UP", double changePct = 5d, double confidence = 0.8d,
        string modelVersion = "modelaaa", string scalerHash = "scalerbbb") =>
        _reproducer.ReproduceAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ReproduceResult(direction, changePct, confidence, modelVersion, scalerHash));

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPredictionMissing()
    {
        _repository.GetPredictionForAuditAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PredictionAudit?)null);

        var result = await _handler.Handle(new ReproducePredictionQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        await _reproducer.DidNotReceive().ReproduceAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenPredictionPredatesSnapshotting()
    {
        StockPrediction legacy = Prediction(featuresJson: null, modelVersion: null, scalerHash: null);
        Arrange(legacy);

        var result = await _handler.Handle(new ReproducePredictionQuery(legacy.Id), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("no stored feature snapshot");
        // Never call the pipeline for something that can't be reproduced.
        await _reproducer.DidNotReceive().ReproduceAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReportMatch_WhenRecomputationAgrees()
    {
        StockPrediction p = Prediction();
        Arrange(p);
        Recomputes();

        var result = await _handler.Handle(new ReproducePredictionQuery(p.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Matches.Should().BeTrue();
        result.Value.Mismatches.Should().BeEmpty();
        result.Value.ModelVersionMatches.Should().BeTrue();
        result.Value.ScalerHashMatches.Should().BeTrue();
        result.Value.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task Handle_Should_TolerateRoundingNoise()
    {
        // The pipeline rounds to 4dp; a difference at that scale is representation
        // noise, not a behaviour change.
        StockPrediction p = Prediction(changePct: 5.0000d);
        Arrange(p);
        Recomputes(changePct: 5.00005d);

        var result = await _handler.Handle(new ReproducePredictionQuery(p.Id), CancellationToken.None);

        result.Value.Matches.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_FlagDirectionFlip()
    {
        StockPrediction p = Prediction(direction: "UP");
        Arrange(p);
        Recomputes(direction: "DOWN");

        var result = await _handler.Handle(new ReproducePredictionQuery(p.Id), CancellationToken.None);

        result.Value.Matches.Should().BeFalse();
        result.Value.Mismatches.Should().ContainSingle(m => m.Contains("direction"));
    }

    [Fact]
    public async Task Handle_Should_FlagEveryDrifitingField()
    {
        StockPrediction p = Prediction(direction: "UP", changePct: 5d, confidence: 0.8d);
        Arrange(p);
        Recomputes(direction: "DOWN", changePct: -2d, confidence: 0.1d);

        var result = await _handler.Handle(new ReproducePredictionQuery(p.Id), CancellationToken.None);

        result.Value.Matches.Should().BeFalse();
        result.Value.Mismatches.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Should_ReportArtifactDrift_WithoutFailingTheAudit()
    {
        // Reproducing an old prediction under new artifacts is a legitimate use:
        // it is how you demonstrate what a model change actually did.
        StockPrediction p = Prediction(modelVersion: "oldmodel", scalerHash: "oldscaler");
        Arrange(p);
        Recomputes(modelVersion: "newmodel", scalerHash: "newscaler");

        var result = await _handler.Handle(new ReproducePredictionQuery(p.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Matches.Should().BeTrue();          // outputs still agree
        result.Value.ModelVersionMatches.Should().BeFalse();
        result.Value.ScalerHashMatches.Should().BeFalse();
        result.Value.Stored.ModelVersion.Should().Be("oldmodel");
        result.Value.Recomputed.ModelVersion.Should().Be("newmodel");
    }
}
