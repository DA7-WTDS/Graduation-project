using FluentAssertions;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Portfolios.CreatePortfolio;
using Project.Modules.Portfolio.Domain.Portfolios;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PortfolioEntity = Project.Modules.Portfolio.Domain.Portfolios.Portfolio;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios.CreatePortfolio;

public class CreatePortfolioCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreatePortfolioCommandHandler _handler;

    public CreatePortfolioCommandHandlerTests()
    {
        _portfolioRepository = Substitute.For<IPortfolioRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new CreatePortfolioCommandHandler(_portfolioRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPortfolioAlreadyExists()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, "Aggressive", 10000m);

        var existingPortfolio = PortfolioEntity.Create(
            command.UserId, command.PrimaryGoal, command.TimeHorizon, command.RiskTolerance,
            command.MarketReaction, command.InvestmentExperience, command.StocksPercentage,
            command.BondsPercentage, command.EtfsPercentage, command.CashPercentage, RiskProfile.Aggressive);

        _portfolioRepository.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingPortfolio);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == PortfolioErrors.PortfolioAlreadyExists(command.UserId).Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRiskProfileIsInvalid()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, "InvalidProfile", 10000m);

        _portfolioRepository.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((PortfolioEntity)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == PortfolioErrors.InvalidRiskProfile.Message);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessAndSave_WhenValid()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Guid.NewGuid(), "Growth", "Long Term", 5, "Buy", "Expert", 80, 10, 5, 5, "Aggressive", 10000m);

        _portfolioRepository.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((PortfolioEntity)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _portfolioRepository.Received(1).AddAsync(Arg.Any<PortfolioEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
