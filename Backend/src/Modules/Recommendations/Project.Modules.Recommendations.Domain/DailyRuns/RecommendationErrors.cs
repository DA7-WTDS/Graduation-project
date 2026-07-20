using FluentResults;
using Project.Common.Domain;
using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.DailyRuns;

public static class RecommendationErrors
{
    public static Error NoRunAvailable =>
        new Error("No prediction run is available yet.")
            .WithErrorType(ErrorType.NotFound);

    public static Error ProfileNotFound(Guid userId) =>
        new Error($"No risk profile found for user {userId}. Complete the onboarding questionnaire first.")
            .WithErrorType(ErrorType.NotFound);

    public static Error LlmUnavailable =>
        new Error("The recommendation engine is temporarily unavailable. Please try again shortly.")
            .WithErrorType(ErrorType.Problem);

    public static Error InvalidIngestPayload(string reason) =>
        new Error($"Invalid ingest payload: {reason}.")
            .WithErrorType(ErrorType.Validation);

    public static Error Unauthorized =>
        new Error("Invalid or missing pipeline key.")
            .WithErrorType(ErrorType.Unauthorized);

    public static Error RunNotFound(Guid runId) =>
        new Error($"Daily run {runId} was not found.")
            .WithErrorType(ErrorType.NotFound);

    public static Error PredictionNotFound(Guid predictionId) =>
        new Error($"Prediction {predictionId} was not found.")
            .WithErrorType(ErrorType.NotFound);

    // Problem, not Validation: this is not a field error, and the caller needs to
    // read *why* the audit cannot run (Validation hides the message by design).
    public static Error PredictionNotReproducible(Guid predictionId) =>
        new Error($"Prediction {predictionId} has no stored feature snapshot — it predates § 6.3 audit capture.")
            .WithErrorType(ErrorType.Problem);

    public static Error InvalidStatusTransition(DailyRunStatus from, DailyRunStatus to) =>
        new Error($"Cannot move a daily run from {from} to {to}.")
            .WithErrorType(ErrorType.Validation);
}
