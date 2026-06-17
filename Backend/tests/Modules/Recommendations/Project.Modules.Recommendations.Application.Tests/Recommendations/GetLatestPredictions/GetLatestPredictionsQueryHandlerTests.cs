using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Recommendations.GetLatestPredictions;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.Recommendations.GetLatestPredictions;

public class GetLatestPredictionsQueryHandlerTests
{
    private readonly IDailyRunRepository _dailyRunRepository = Substitute.For<IDailyRunRepository>();
    private readonly GetLatestPredictionsQueryHandler _handler;

    public GetLatestPredictionsQueryHandlerTests()
    {
        _handler = new GetLatestPredictionsQueryHandler(_dailyRunRepository);
    }

    private static StockPrediction Prediction(string ticker, double conviction) =>
        StockPrediction.Create(ticker, "UP", 5d, 0.8d, 0.5d, "POSITIVE", 4.5d, "Buy", 0.15d, 0.9d,
            "CONFIRMED", "LOW", conviction, new[] { "none" }, "rationale");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenNoRun()
    {
        _dailyRunRepository.GetLatestAsync(true, Arg.Any<CancellationToken>()).Returns((DailyRun)null);

        var result = await _handler.Handle(new GetLatestPredictionsQuery(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRunHasNoPredictions()
    {
        var run = DailyRun.Create(DateTime.UtcNow, new List<StockPrediction>());
        _dailyRunRepository.GetLatestAsync(true, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(new GetLatestPredictionsQuery(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnPredictions_OrderedByConvictionDescending()
    {
        var run = DailyRun.Create(DateTime.UtcNow, new List<StockPrediction>
        {
            Prediction("LOWC", 0.40),
            Prediction("HIGHC", 0.95),
        });
        _dailyRunRepository.GetLatestAsync(true, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(new GetLatestPredictionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Predictions.Should().HaveCount(2);
        result.Value.Predictions[0].Ticker.Should().Be("HIGHC");
        result.Value.Predictions[1].Ticker.Should().Be("LOWC");
        result.Value.GeneratedAt.Should().Be(run.GeneratedAt);
    }
}
