using FluentAssertions;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Goals.SubmitQuestionnaire;
using Project.Modules.Portfolio.Domain.Goals;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Goals;

public class SubmitQuestionnaireCommandHandlerTests
{
    private readonly IGoalRepository _goalRepository = Substitute.For<IGoalRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SubmitQuestionnaireCommandHandler _handler;

    public SubmitQuestionnaireCommandHandlerTests()
    {
        _handler = new SubmitQuestionnaireCommandHandler(_goalRepository, _unitOfWork);
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
    public async Task The_amount_is_stored_on_the_goal_for_the_optimizer_to_size_against()
    {
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(Command(userId), CancellationToken.None);

        result.Value.InvestmentAmount.Should().Be(10000m);
        await _goalRepository.Received(1).AddGoalAsync(
            Arg.Is<Goal>(g => g.InvestmentAmount == 10000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retake_appends_the_next_profile_version_and_redefines_the_goal_in_place()
    {
        var userId = Guid.NewGuid();
        Goal goal = Goal.Create(userId, GoalType.Retirement, 10, 5000m);
        _goalRepository.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>()).Returns(goal);

        var previousProfile = InvestorProfile.Create(
            goal.Id, Guid.NewGuid(), 2,
            new RiskScore(50, 50, 50, Project.Modules.Portfolio.Domain.Portfolios.RiskProfile.Moderate, false),
            EngagementLevel.Monthly, UsdComfort.Neutral, RiskScoring.Version);
        _goalRepository.GetLatestProfileAsync(goal.Id, Arg.Any<CancellationToken>()).Returns(previousProfile);

        var result = await _handler.Handle(
            Command(userId, goalId: goal.Id, goalType: "long_term_wealth"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileVersion.Should().Be(3);

        // The goal is redefined in place — never forked into a second row.
        goal.Type.Should().Be(GoalType.LongTermWealth);
        goal.InvestmentAmount.Should().Be(10000m);
        await _goalRepository.DidNotReceive().AddGoalAsync(Arg.Any<Goal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retaking_someone_elses_goal_is_rejected()
    {
        Goal goal = Goal.Create(Guid.NewGuid(), GoalType.Retirement, 10, 10000m);
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
