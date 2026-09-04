using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Configuration;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Recommendations.Domain.DailyRuns;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.DailyRuns.Ingest;

public class IngestDailyRunCommandHandlerTests
{
    private readonly IDailyRunRepository _dailyRunRepository;
    private readonly IngestOptions _ingestOptions = new();
    private readonly IUnitOfWork _unitOfWork;
    private readonly IngestDailyRunCommandHandler _handler;

    public IngestDailyRunCommandHandlerTests()
    {
        _dailyRunRepository = Substitute.For<IDailyRunRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new IngestDailyRunCommandHandler(_dailyRunRepository, Options.Create(_ingestOptions), _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRecordsAreNull()
    {
        // Arrange
        var command = new IngestDailyRunCommand(DateTime.UtcNow, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == RecommendationErrors.InvalidIngestPayload("no records provided").Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnExistingId_WhenRunAlreadyExists()
    {
        // Arrange
        var command = new IngestDailyRunCommand(DateTime.UtcNow, new List<PredictionRecordDto> { new() });
        var existingRun = DailyRun.Create(command.GeneratedAt, new List<StockPrediction>());

        _dailyRunRepository.GetByGeneratedAtAsync(command.GeneratedAt, Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(existingRun);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingRun.Id);

        await _dailyRunRepository.DidNotReceive().AddAsync(Arg.Any<DailyRun>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateNewRunAndReturnId_WhenValid()
    {
        // Arrange
        var record = new PredictionRecordDto 
        { 
            Ticker = "AAPL", Direction = "Bullish", ChangePct = 5.0, Confidence = 0.8, SentimentScore = 0.5,
            Signal = "Buy", AnalystRating = 4.5, RatingLabel = "Strong Buy", PtUpsidePct = 0.15,
            NewsScore = 0.9, Agreement = "High", RiskLevel = "Low", ConvictionScore = 0.95,
            RiskFlags = new[] { "None" }, Rationale = "Strong earnings" 
        };
        var command = new IngestDailyRunCommand(DateTime.UtcNow, new List<PredictionRecordDto> { record });

        _dailyRunRepository.GetByGeneratedAtAsync(command.GeneratedAt, Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((DailyRun)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _dailyRunRepository.Received(1).AddAsync(Arg.Any<DailyRun>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static PredictionRecordDto Record() => new()
    {
        Ticker = "AAPL", Direction = "UP", ChangePct = 5.0, Confidence = 0.8, SentimentScore = 0.5,
        Signal = "POSITIVE", Agreement = "CONFIRMED", RiskLevel = "LOW", ConvictionScore = 0.95,
        RiskFlags = new[] { "None" }, Rationale = "test"
    };

    [Fact]
    public async Task Handle_Should_LandAsPublished_WhenGatesPassAndAutoApprove()
    {
        DailyRun? captured = null;
        _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        var command = new IngestDailyRunCommand(DateTime.UtcNow, new List<PredictionRecordDto> { Record() });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured!.Status.Should().Be(DailyRunStatus.Published);
    }

    [Fact]
    public async Task Handle_Should_LandAsPendingReview_WhenManualApprovalRequired()
    {
        _ingestOptions.RequireManualApproval = true;
        DailyRun? captured = null;
        _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        var command = new IngestDailyRunCommand(DateTime.UtcNow, new List<PredictionRecordDto> { Record() });
        await _handler.Handle(command, CancellationToken.None);

        captured!.Status.Should().Be(DailyRunStatus.PendingReview);
    }

    [Fact]
    public async Task Handle_Should_LandAsQuarantined_WhenGatesFailed_EvenWithAutoApprove()
    {
        DailyRun? captured = null;
        _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        var command = new IngestDailyRunCommand(
            DateTime.UtcNow,
            new List<PredictionRecordDto> { Record() },
            GatesPassed: false,
            GateFailures: new[] { "coverage: 40/100 (40%, min 60%)" });
        await _handler.Handle(command, CancellationToken.None);

        captured!.Status.Should().Be(DailyRunStatus.Quarantined);
        captured.StatusReason.Should().Contain("coverage");
    }

    // ---- § C fidelity lane: replayed runs -------------------------------------

    [Fact]
    public async Task Simulated_run_lands_as_Simulated_even_when_manual_approval_is_on()
    {
        _ingestOptions.RequireManualApproval = true;
        var command = new IngestDailyRunCommand(
            DateTime.UtcNow, new List<PredictionRecordDto> { new() }, Simulated: true);

        DailyRun? captured = null;
        await _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured!.Status.Should().Be(DailyRunStatus.Simulated);
    }

    [Fact]
    public async Task A_replayed_run_that_failed_gates_is_still_Simulated_not_Quarantined()
    {
        // Quarantined is promotable by an operator; Simulated is not. A manufactured
        // record must never be one approval click away from a user.
        var command = new IngestDailyRunCommand(
            DateTime.UtcNow, new List<PredictionRecordDto> { new() },
            GatesPassed: false, GateFailures: new List<string> { "coverage" }, Simulated: true);

        DailyRun? captured = null;
        await _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        await _handler.Handle(command, CancellationToken.None);

        captured!.Status.Should().Be(DailyRunStatus.Simulated);
        captured.StatusReason.Should().Contain("coverage");
    }

    [Fact]
    public async Task A_replayed_run_raises_no_ingest_event()
    {
        // The ingest event fans out to an ops alert per Admin. Backfilling a year would
        // deliver several hundred, which is how an alert channel gets muted for good.
        var command = new IngestDailyRunCommand(
            DateTime.UtcNow, new List<PredictionRecordDto> { new() }, Simulated: true);

        DailyRun? captured = null;
        await _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        await _handler.Handle(command, CancellationToken.None);

        captured!.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task A_live_run_still_raises_its_ingest_event()
    {
        var command = new IngestDailyRunCommand(DateTime.UtcNow, new List<PredictionRecordDto> { new() });

        DailyRun? captured = null;
        await _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        await _handler.Handle(command, CancellationToken.None);

        captured!.DomainEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Idempotency_is_scoped_so_a_replay_does_not_collide_with_the_live_run()
    {
        // The § C backfill covers a year that live runs also occupy. If the lookup were
        // not scoped, the replay would find the live run, return ITS id, and be silently
        // dropped — leaving a gap in the manufactured history that nothing reports.
        var generatedAt = DateTime.UtcNow;
        var command = new IngestDailyRunCommand(
            generatedAt, new List<PredictionRecordDto> { new() }, Simulated: true);

        await _handler.Handle(command, CancellationToken.None);

        await _dailyRunRepository.Received(1).GetByGeneratedAtAsync(
            Arg.Any<DateTime>(), Arg.Any<string>(), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Market_is_carried_onto_the_run()
    {
        var command = new IngestDailyRunCommand(
            DateTime.UtcNow, new List<PredictionRecordDto> { new() }, Market: "egx");

        DailyRun? captured = null;
        await _dailyRunRepository.AddAsync(Arg.Do<DailyRun>(r => captured = r), Arg.Any<CancellationToken>());

        await _handler.Handle(command, CancellationToken.None);

        captured!.Market.Should().Be("egx");
    }

}
