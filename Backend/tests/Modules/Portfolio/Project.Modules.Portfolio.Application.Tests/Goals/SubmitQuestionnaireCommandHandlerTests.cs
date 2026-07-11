using FluentAssertions;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Goals.SubmitQuestionnaire;
using Project.Modules.Portfolio.Domain.Goals;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PortfolioEntity = Project.Modules.Portfolio.Domain.Portfolios.Portfolio;

namespace Project.Modules.Portfolio.Application.Tests.Goals;

public class SubmitQuestionnaireCommandHandlerTests
{
    private readonly IGoalRepository _goalRepository = Substitute.For<IGoalRepository>();
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SubmitQuestionnaireCommandHandler _handler;

    public SubmitQuestionnaireCommandHandlerTests()
    {
        _handler = new SubmitQuestionnaireCommandHandler(_goalRepository, _portfolioRepository, _unitOfWork);
    }

    private static SubmitQuestionnaireCommand Command(
        Guid userId,
        Guid? goalId = null,
        string goalType = "retirement",
        string marketReaction = "buy_more",
        string experience = "experienced") =>
        new(userId, goalId, goalType,
            HorizonYears: 10,
            InvestmentAmount: 10000m,
            MonthlyContribution: 500m,
            HasEmergencyFund: true,
            IncomeStability: "stable",
            SavingsShare: "less_than_ten_percent",
            MarketReaction: marketReaction,
            Experience: experience,
            Engagement: "set_and_forget",
            UsdComfort: "comfortable",
            AffordLossConfirmed: false);

    [Fact]
    public async Task First_submission_creates_goal_response_and_profile_v1()
    {
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(Command(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileVersion.Should().Be(1);
        result.Value.RiskBand.Should().Be("Aggressive");
        result.Value.EffectiveRisk.Should().Be(100);

        await _goalRepository.Received(1).AddGoalAsync(
            Arg.Is<Goal>(g => g.UserId == userId && g.Type == GoalType.Retirement && g.HorizonYears == 10),
            Arg.Any<CancellationToken>());
        await _goalRepository.Received(1).AddResponseAsync(
            Arg.Is<QuestionnaireResponse>(q => q.ScoringVersion == RiskScoring.Version),
            Arg.Any<CancellationToken>());
        await _goalRepository.Received(1).AddProfileAsync(
            Arg.Is<InvestorProfile>(p => p.Version == 1 && p.EffectiveRisk == 100),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submission_bridges_a_legacy_portfolio_with_server_derived_allocation()
    {
        var userId = Guid.NewGuid();
        _portfolioRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((PortfolioEntity?)null);

        var result = await _handler.Handle(Command(userId), CancellationToken.None);

        // Aggressive band → the same coarse split the old client-side calc produced.
        result.Value.StocksPercentage.Should().Be(60);
        result.Value.BondsPercentage.Should().Be(20);
        result.Value.EtfsPercentage.Should().Be(15);
        result.Value.CashPercentage.Should().Be(5);

        await _portfolioRepository.Received(1).AddAsync(
            Arg.Is<PortfolioEntity>(p =>
                p.UserId == userId
                && p.StocksPercentage == 60
                && p.RiskProfile == Project.Modules.Portfolio.Domain.Portfolios.RiskProfile.Aggressive
                && p.InvestmentAmount == 10000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retake_appends_the_next_profile_version_and_updates_the_existing_portfolio()
    {
        var userId = Guid.NewGuid();
        Goal goal = Goal.Create(userId, GoalType.Retirement, 10);
        _goalRepository.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>()).Returns(goal);

        var previousProfile = InvestorProfile.Create(
            goal.Id, Guid.NewGuid(), 2,
            new RiskScore(50, 50, 50, Project.Modules.Portfolio.Domain.Portfolios.RiskProfile.Moderate, false),
            EngagementLevel.Monthly, UsdComfort.Neutral, RiskScoring.Version);
        _goalRepository.GetLatestProfileAsync(goal.Id, Arg.Any<CancellationToken>()).Returns(previousProfile);

        PortfolioEntity existingPortfolio = PortfolioEntity.Create(
            userId, "Retirement", "long", 50, "HoldSteady", "Beginner",
            40, 35, 20, 5, Project.Modules.Portfolio.Domain.Portfolios.RiskProfile.Moderate, 5000m);
        _portfolioRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existingPortfolio);

        var result = await _handler.Handle(
            Command(userId, goalId: goal.Id, goalType: "long_term_wealth"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileVersion.Should().Be(3);
        goal.Type.Should().Be(GoalType.LongTermWealth);

        // Existing portfolio row mutated in place — never a second row per user.
        await _portfolioRepository.DidNotReceive().AddAsync(
            Arg.Any<PortfolioEntity>(), Arg.Any<CancellationToken>());
        existingPortfolio.StocksPercentage.Should().Be(60);
        existingPortfolio.InvestmentAmount.Should().Be(10000m);
    }

    [Fact]
    public async Task Retaking_someone_elses_goal_is_rejected()
    {
        Goal goal = Goal.Create(Guid.NewGuid(), GoalType.Retirement, 10);
        _goalRepository.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>()).Returns(goal);

        var result = await _handler.Handle(
            Command(Guid.NewGuid(), goalId: goal.Id), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == GoalErrors.UnauthorizedAccess.Message);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_goal_id_is_not_found()
    {
        var goalId = Guid.NewGuid();
        _goalRepository.GetByIdAsync(goalId, Arg.Any<CancellationToken>()).Returns((Goal?)null);

        var result = await _handler.Handle(
            Command(Guid.NewGuid(), goalId: goalId), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == GoalErrors.GoalNotFound(goalId).Message);
    }

    [Theory]
    [InlineData("goalType", "day_trading_to_the_moon")]
    [InlineData("marketReaction", "cry")]
    [InlineData("experience", "wolf_of_wall_street")]
    public async Task Invalid_answer_tokens_fail_validation_and_persist_nothing(string field, string value)
    {
        SubmitQuestionnaireCommand command = field switch
        {
            "goalType" => Command(Guid.NewGuid(), goalType: value),
            "marketReaction" => Command(Guid.NewGuid(), marketReaction: value),
            _ => Command(Guid.NewGuid(), experience: value)
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        await _goalRepository.DidNotReceive().AddGoalAsync(Arg.Any<Goal>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Snake_case_and_pascal_case_tokens_are_both_accepted()
    {
        var result1 = await _handler.Handle(
            Command(Guid.NewGuid(), goalType: "long_term_wealth"), CancellationToken.None);
        var result2 = await _handler.Handle(
            Command(Guid.NewGuid(), goalType: "LongTermWealth"), CancellationToken.None);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
    }
}
