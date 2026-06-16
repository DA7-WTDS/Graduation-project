using FluentAssertions;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Portfolios.UpdatePortfolio;
using Project.Modules.Portfolio.Domain.Portfolios;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PortfolioEntity = Project.Modules.Portfolio.Domain.Portfolios.Portfolio;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios.UpdatePortfolio;

public class UpdatePortfolioCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdatePortfolioCommandHandler _handler;

    public UpdatePortfolioCommandHandlerTests()
    {
        _portfolioRepository = Substitute.For<IPortfolioRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new UpdatePortfolioCommandHandler(_portfolioRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPortfolioNotFound()
    {
        // Arrange
        var command = new UpdatePortfolioCommand(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, "Aggressive", 10000m);

        _portfolioRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((PortfolioEntity)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == PortfolioErrors.PortfolioNotFound(command.Id).Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRiskProfileIsInvalid()
    {
        // Arrange
        var command = new UpdatePortfolioCommand(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, "InvalidProfile", 10000m);

        var existingPortfolio = PortfolioEntity.Create(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, RiskProfile.Aggressive);

        _portfolioRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(existingPortfolio);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == PortfolioErrors.InvalidRiskProfile.Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessAndUpdate_WhenValid()
    {
        // Arrange
        var command = new UpdatePortfolioCommand(
            Guid.NewGuid(), "Value", "Short Term", 2, "Sell", "Beginner", 50, 30, 10, 10, "Conservative", 10000m);

        var existingPortfolio = PortfolioEntity.Create(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, RiskProfile.Aggressive);

        _portfolioRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(existingPortfolio);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _portfolioRepository.Received(1).Update(Arg.Any<PortfolioEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
