using FluentResults;
using Project.Common.Domain;
using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Portfolios;

public static class PortfolioErrors
{
    // Not Found Errors
    public static Error PortfolioNotFound(Guid portfolioId) =>
        new Error($"Portfolio with ID {portfolioId} not found.")
            .WithErrorType(ErrorType.NotFound);

    public static Error PortfolioNotFoundForUser(Guid userId) =>
        new Error($"Portfolio for user with ID {userId} not found.")
            .WithErrorType(ErrorType.NotFound);

    // Conflict Errors
    public static Error PortfolioAlreadyExists(Guid userId) =>
        new Error($"Portfolio for user with ID {userId} already exists.")
            .WithErrorType(ErrorType.Conflict);

    // Authorization Errors
    public static Error UnauthorizedAccess =>
        new Error("You are not authorized to access this portfolio.")
            .WithErrorType(ErrorType.Unauthorized);

    // Validation Errors - Primary Goal
    public static Error InvalidPrimaryGoal(string value) =>
        new Error($"Invalid primary goal '{value}'. Must be one of: retirement, property, education, wealth.")
            .WithErrorType(ErrorType.Validation);

    // Validation Errors - Time Horizon
    public static Error InvalidTimeHorizon(string value) =>
        new Error($"Invalid time horizon '{value}'. Must be one of: short, medium, long.")
            .WithErrorType(ErrorType.Validation);

    // Validation Errors - Risk Tolerance
    public static Error InvalidRiskTolerance(int value) =>
        new Error($"Invalid risk tolerance '{value}'. Must be between 0 and 100.")
            .WithErrorType(ErrorType.Validation);

    // Validation Errors - Market Reaction
    public static Error InvalidMarketReaction(string value) =>
        new Error($"Invalid market reaction '{value}'. Must be one of: aggressive, moderate, conservative.")
            .WithErrorType(ErrorType.Validation);

    // Validation Errors - Investment Experience
    public static Error InvalidInvestmentExperience(string value) =>
        new Error($"Invalid investment experience '{value}'. Must be one of: high, medium, low.")
            .WithErrorType(ErrorType.Validation);

    // Validation Errors - Percentages
    public static Error InvalidPercentage(string field, int value) =>
        new Error($"Invalid {field} percentage '{value}'. Must be between 0 and 100.")
            .WithErrorType(ErrorType.Validation);

    public static Error InvalidAllocationTotal(int total) =>
        new Error($"Portfolio allocation percentages must sum to 100. Current total: {total}%.")
            .WithErrorType(ErrorType.Validation);

    // Validation Errors - Risk Profile
    public static Error InvalidRiskProfile =>
        new Error("Invalid risk profile. Must be Conservative, Moderate, or Aggressive.")
            .WithErrorType(ErrorType.Validation);
}
