namespace Project.Modules.Portfolio.Domain.Goals;

public enum IncomeStability
{
    None,
    Variable,
    Stable
}

public enum SavingsShareBand
{
    LessThanTenPercent,
    TenToTwentyFivePercent,
    TwentyFiveToFiftyPercent,
    MoreThanFiftyPercent
}

public enum MarketReactionAnswer
{
    SellAll,
    SellSome,
    HoldSteady,
    BuyMore
}

public enum ExperienceLevel
{
    None,
    Beginner,
    Intermediate,
    Experienced
}

public enum EngagementLevel
{
    SetAndForget,
    Monthly,
    Daily
}

public enum UsdComfort
{
    PreferEgp,
    Neutral,
    Comfortable
}

/// <summary>
/// The raw questionnaire answers (one record = one submission). Serialized as-is
/// into the questionnaire_responses table so the suitability record is complete
/// even if the scoring rules change later.
/// </summary>
public sealed record QuestionnaireAnswers(
    GoalType GoalType,
    int HorizonYears,
    decimal InvestmentAmount,
    decimal MonthlyContribution,
    bool HasEmergencyFund,
    IncomeStability IncomeStability,
    SavingsShareBand SavingsShare,
    MarketReactionAnswer MarketReaction,
    ExperienceLevel Experience,
    EngagementLevel Engagement,
    UsdComfort UsdComfort,
    bool AffordLossConfirmed);
