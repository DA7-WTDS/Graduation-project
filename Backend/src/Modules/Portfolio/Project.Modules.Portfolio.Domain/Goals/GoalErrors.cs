using FluentResults;
using Project.Common.Domain;
using Project.Common.Domain.Abstractions;

namespace Project.Modules.Portfolio.Domain.Goals;

public static class GoalErrors
{
    public static Error GoalNotFound(Guid goalId) =>
        new Error($"Goal with ID {goalId} not found.")
            .WithErrorType(ErrorType.NotFound);

    public static Error UnauthorizedAccess =>
        new Error("You are not authorized to access this goal.")
            .WithErrorType(ErrorType.Forbidden);

    public static Error InvalidAnswer(string field, string value, string allowed) =>
        new Error($"Invalid {field} '{value}'. Must be one of: {allowed}.")
            .WithErrorType(ErrorType.Validation);

    public static Error InvalidHorizon(int value) =>
        new Error($"Invalid horizon '{value}' years. Must be between 0 and 60.")
            .WithErrorType(ErrorType.Validation);

    public static Error InvalidAmount(decimal value) =>
        new Error($"Invalid amount '{value}'. Must be zero or greater.")
            .WithErrorType(ErrorType.Validation);

    public static Error ProfileMissing =>
        new Error("Complete the questionnaire before requesting a portfolio draft.")
            .WithErrorType(ErrorType.Validation);

    public static Error NoTemplateMatches =>
        new Error("No strategy template matches this profile — seed data is incomplete.")
            .WithErrorType(ErrorType.NotFound);
}
