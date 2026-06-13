using FluentAssertions;
using NSubstitute;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IngestDailyRunCommandHandler _handler;

    public IngestDailyRunCommandHandlerTests()
    {
        _dailyRunRepository = Substitute.For<IDailyRunRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new IngestDailyRunCommandHandler(_dailyRunRepository, _unitOfWork);
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

        _dailyRunRepository.GetByGeneratedAtAsync(command.GeneratedAt, Arg.Any<CancellationToken>())
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

        _dailyRunRepository.GetByGeneratedAtAsync(command.GeneratedAt, Arg.Any<CancellationToken>())
            .Returns((DailyRun)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _dailyRunRepository.Received(1).AddAsync(Arg.Any<DailyRun>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
