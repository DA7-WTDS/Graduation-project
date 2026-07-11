using Project.Common.Application.Messaging;

namespace Project.Modules.Portfolio.Application.Goals.SubmitQuestionnaire;

/// <summary>
/// One submission of the onboarding questionnaire (§ 2.1). Raw answers only —
/// the server derives capacity/tolerance/effective risk; the client computes nothing.
/// GoalId is null on first submission and set on retakes (new profile version).
/// </summary>
public sealed record SubmitQuestionnaireCommand(
    Guid UserId,
    Guid? GoalId,
    string GoalType,
    int HorizonYears,
    decimal InvestmentAmount,
    decimal MonthlyContribution,
    bool HasEmergencyFund,
    string IncomeStability,
    string SavingsShare,
    string MarketReaction,
    string Experience,
    string Engagement,
    string UsdComfort,
    bool AffordLossConfirmed) : ICommand<SubmitQuestionnaireResponse>;
