using FluentAssertions;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Portfolios.GetPortfolio;
using Project.Modules.Portfolio.Domain.Portfolios;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PortfolioEntity = Project.Modules.Portfolio.Domain.Portfolios.Portfolio;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios.GetPortfolio;

public class GetPortfolioQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly GetPortfolioQueryHandler _handler;

    public GetPortfolioQueryHandlerTests()
    {
        _portfolioRepository = Substitute.For<IPortfolioRepository>();
        _handler = new GetPortfolioQueryHandler(_portfolioRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPortfolioNotFound()
    {
        // Arrange
        var query = new GetPortfolioQuery(Guid.NewGuid());

        _portfolioRepository.GetByIdAsync(query.PortfolioId, Arg.Any<CancellationToken>())
            .Returns((PortfolioEntity)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == PortfolioErrors.PortfolioNotFound(query.PortfolioId).Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnPortfolioResponse_WhenFound()
    {
        // Arrange
        var query = new GetPortfolioQuery(Guid.NewGuid());
        var portfolio = PortfolioEntity.Create(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, RiskProfile.Aggressive);
            
        // Assuming Id is set privately, but for the sake of the record mapping test we just return it
        // The mock will return the portfolio which has an empty or new Guid as Id, it's fine for the test.

        _portfolioRepository.GetByIdAsync(query.PortfolioId, Arg.Any<CancellationToken>())
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
