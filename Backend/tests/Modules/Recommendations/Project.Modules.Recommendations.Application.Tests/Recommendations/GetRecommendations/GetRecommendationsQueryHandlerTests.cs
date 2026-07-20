using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Project.Common.Application.Caching;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.Holdings;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.Abstractions.Outcomes;
using Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Domain.Holdings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.Recommendations.GetRecommendations;

public class GetRecommendationsQueryHandlerTests
{
    private readonly IDailyRunRepository _dailyRunRepository;
    private readonly IUserHoldingRepository _holdingRepository;
    private readonly IPredictionOutcomeRepository _outcomeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPortfolioApi _portfolioApi;
    private readonly ILlmClient _llmClient;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetRecommendationsQueryHandler> _logger;
    private readonly GetRecommendationsQueryHandler _handler;

    public GetRecommendationsQueryHandlerTests()
    {
        _dailyRunRepository = Substitute.For<IDailyRunRepository>();
        _holdingRepository = Substitute.For<IUserHoldingRepository>();
        _outcomeRepository = Substitute.For<IPredictionOutcomeRepository>();
        _outcomeRepository.GetSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<OutcomeStat>)new List<OutcomeStat>());
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _portfolioApi = Substitute.For<IPortfolioApi>();
        _llmClient = Substitute.For<ILlmClient>();
        _cacheService = Substitute.For<ICacheService>();
        _logger = Substitute.For<ILogger<GetRecommendationsQueryHandler>>();

        _holdingRepository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<UserHolding>)new List<UserHolding>());

        _handler = new GetRecommendationsQueryHandler(
            _dailyRunRepository,
            _holdingRepository,
            _outcomeRepository,
            _unitOfWork,
            _portfolioApi,
            _llmClient,
            _cacheService,
            _logger);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenNoRunAvailable()
    {
        // Arrange
        var query = new GetRecommendationsQuery(Guid.NewGuid());

        _dailyRunRepository.GetLatestPublishedAsync(true, Arg.Any<CancellationToken>())
            .Returns((DailyRun)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == RecommendationErrors.NoRunAvailable.Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPortfolioNotFound()
    {
        // Arrange
        var query = new GetRecommendationsQuery(Guid.NewGuid());
        var predictions = new List<StockPrediction> 
        { 
            StockPrediction.Create("AAPL", "Bullish", 5d, 0.8d, 0.5d, "Buy", 4.5d, "Strong Buy", 0.15d, 0.9d, "High", "Low", 0.95d, new[] { "None" }, "Reason") 
        };
        var run = DailyRun.Create(DateTime.UtcNow, predictions);

        _dailyRunRepository.GetLatestPublishedAsync(true, Arg.Any<CancellationToken>())
            .Returns(run);

        _portfolioApi.GetMonitoringProfileAsync(query.UserId, Arg.Any<CancellationToken>())
            .Returns((MonitoringProfileResponse)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == RecommendationErrors.ProfileNotFound(query.UserId).Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnCachedResponse_WhenCacheHit()
    {
        // Arrange
        var query = new GetRecommendationsQuery(Guid.NewGuid());
        var predictions = new List<StockPrediction> 
        { 
            StockPrediction.Create("AAPL", "Bullish", 5d, 0.8d, 0.5d, "Buy", 4.5d, "Strong Buy", 0.15d, 0.9d, "High", "Low", 0.95d, new[] { "None" }, "Reason") 
        };
        var run = DailyRun.Create(DateTime.UtcNow, predictions);

        _dailyRunRepository.GetLatestPublishedAsync(true, Arg.Any<CancellationToken>())
            .Returns(run);

        var portfolio = new MonitoringProfileResponse(query.UserId, "Aggressive", "LongTermWealth", "Monthly");
        _portfolioApi.GetMonitoringProfileAsync(query.UserId, Arg.Any<CancellationToken>())
            .Returns(portfolio);

        var cachedResponse = new RecommendationResponse("Summary", new List<RecommendationItem>(), DateTime.UtcNow);
        
        _cacheService.GetAsync<RecommendationResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedResponse);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedResponse);
        await _llmClient.DidNotReceive().CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenLlmThrowsException()
    {
        // Arrange
        var query = new GetRecommendationsQuery(Guid.NewGuid());
        var predictions = new List<StockPrediction> 
        { 
            StockPrediction.Create("AAPL", "Bullish", 5d, 0.8d, 0.5d, "Buy", 4.5d, "Strong Buy", 0.15d, 0.9d, "High", "Low", 0.95d, new[] { "None" }, "Reason") 
        };
        var run = DailyRun.Create(DateTime.UtcNow, predictions);

        _dailyRunRepository.GetLatestPublishedAsync(true, Arg.Any<CancellationToken>())
            .Returns(run);

        var portfolio = new MonitoringProfileResponse(query.UserId, "Aggressive", "LongTermWealth", "Monthly");
        _portfolioApi.GetMonitoringProfileAsync(query.UserId, Arg.Any<CancellationToken>())
            .Returns(portfolio);

        _cacheService.GetAsync<RecommendationResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RecommendationResponse)null);

        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("LLM Error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == RecommendationErrors.LlmUnavailable.Message);
    }
}
