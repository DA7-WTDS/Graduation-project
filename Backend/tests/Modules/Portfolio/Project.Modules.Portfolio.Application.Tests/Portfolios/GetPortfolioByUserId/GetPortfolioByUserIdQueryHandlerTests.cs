using FluentAssertions;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolioByUserId;
using Project.Modules.Portfolio.Domain.Portfolios;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PortfolioEntity = Project.Modules.Portfolio.Domain.Portfolios.Portfolio;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios.GetPortfolioByUserId;

public class GetPortfolioByUserIdQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly GetPortfolioByUserIdQueryHandler _handler;

    public GetPortfolioByUserIdQueryHandlerTests()
    {
        _portfolioRepository = Substitute.For<IPortfolioRepository>();
        _handler = new GetPortfolioByUserIdQueryHandler(_portfolioRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPortfolioNotFound()
    {
        // Arrange
        var query = new GetPortfolioByUserIdQuery(Guid.NewGuid());

        _portfolioRepository.GetByUserIdAsync(query.UserId, Arg.Any<CancellationToken>())
            .Returns((PortfolioEntity)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == PortfolioErrors.PortfolioNotFoundForUser(query.UserId).Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnPortfolioResponse_WhenFound()
    {
        // Arrange
        var query = new GetPortfolioByUserIdQuery(Guid.NewGuid());
        var portfolio = PortfolioEntity.Create(
            query.UserId, "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, RiskProfile.Aggressive);

        _portfolioRepository.GetByUserIdAsync(query.UserId, Arg.Any<CancellationToken>())
            .Returns(portfolio);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(portfolio.UserId);
        result.Value.RiskProfile.Should().Be(portfolio.RiskProfile.ToString());
    }
}
