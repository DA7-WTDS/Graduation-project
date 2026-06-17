using FluentAssertions;
using Project.Modules.Portfolio.Domain.Portfolios;
using Xunit;
using PortfolioEntity = Project.Modules.Portfolio.Domain.Portfolios.Portfolio;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios;

public class PortfolioTests
{
    [Fact]
    public void Create_Should_SetFields_IncludingInvestmentAmount_AndRaiseEvent()
    {
        var userId = Guid.NewGuid();

        var portfolio = PortfolioEntity.Create(
            userId, "wealth", "long", 60, "moderate", "high",
            60, 20, 15, 5, RiskProfile.Moderate, 10000m);

        portfolio.Id.Should().NotBeEmpty();
        portfolio.UserId.Should().Be(userId);
        portfolio.PrimaryGoal.Should().Be("wealth");
        portfolio.StocksPercentage.Should().Be(60);
        portfolio.RiskProfile.Should().Be(RiskProfile.Moderate);
        portfolio.InvestmentAmount.Should().Be(10000m);
        portfolio.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        portfolio.DomainEvents.Should().ContainSingle(e => e is PortfolioCreatedDomainEvent);
    }

    [Fact]
    public void Create_Should_DefaultInvestmentAmountToZero()
    {
        var portfolio = PortfolioEntity.Create(
            Guid.NewGuid(), "wealth", "long", 50, "moderate", "high",
            40, 35, 20, 5, RiskProfile.Conservative);

        portfolio.InvestmentAmount.Should().Be(0m);
    }

    [Fact]
    public void Update_Should_ChangeFields_SetUpdatedAt_AndInvestmentAmount()
    {
        var portfolio = PortfolioEntity.Create(
            Guid.NewGuid(), "wealth", "long", 50, "moderate", "high",
            40, 35, 20, 5, RiskProfile.Conservative, 1000m);

        portfolio.Update(
            "retirement", "short", 80, "aggressive", "low",
            70, 10, 15, 5, RiskProfile.Aggressive, 25000m);

        portfolio.PrimaryGoal.Should().Be("retirement");
        portfolio.TimeHorizon.Should().Be("short");
        portfolio.RiskProfile.Should().Be(RiskProfile.Aggressive);
        portfolio.StocksPercentage.Should().Be(70);
        portfolio.InvestmentAmount.Should().Be(25000m);
        portfolio.UpdatedAt.Should().NotBeNull();
    }
}
