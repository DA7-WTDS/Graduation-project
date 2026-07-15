using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using Project.Common.Application.Caching;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.Holdings;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.Abstractions.Outcomes;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Domain.Holdings;
using static Project.Modules.Recommendations.Domain.DailyRuns.RecommendationErrors;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

internal sealed class GetRecommendationsQueryHandler(
    IDailyRunRepository dailyRunRepository,
    IUserHoldingRepository holdingRepository,
    IPredictionOutcomeRepository outcomeRepository,
    IUnitOfWork unitOfWork,
    IPortfolioApi portfolioApi,
    ILlmClient llmClient,
    ICacheService cacheService,
    ILogger<GetRecommendationsQueryHandler> logger)
    : IQueryHandler<GetRecommendationsQuery, RecommendationResponse>
{
    private const int MaxParseAttempts = 3;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<RecommendationResponse>> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        DailyRun? run = await dailyRunRepository.GetLatestAsync(includePredictions: true, cancellationToken);
        if (run is null || run.Predictions.Count == 0)
        {
            return Result.Fail(NoRunAvailable);
        }

        MonitoringProfileResponse? profile = await portfolioApi.GetMonitoringProfileAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result.Fail(ProfileNotFound(request.UserId));
        }

        string language = string.Equals(request.Language, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
        string cacheKey = $"recommendations:{request.UserId}:{language}:{run.GeneratedAt:yyyyMMddHHmmss}";
        RecommendationResponse? cached = await cacheService.GetAsync<RecommendationResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Ok(cached);
        }

        // Treat the picks from the user's last (prior-run) recommendation as their
        // current holdings, so the assistant can SELL/HOLD them, not just BUY anew.
        IReadOnlyList<UserHolding> existingHoldings = await holdingRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var priorHoldings = existingHoldings.Where(h => h.RunGeneratedAt < run.GeneratedAt).ToList();

        // Context pack extras (§ 3.6): the ONLY performance figures the model
        // may cite — our realized 90-day track record.
        TrackRecordSnippet? trackRecord = null;
        IReadOnlyList<OutcomeStat> outcomes =
            await outcomeRepository.GetSinceAsync(DateTime.UtcNow.AddDays(-90), cancellationToken);
        if (outcomes.Count > 0)
        {
            trackRecord = new TrackRecordSnippet(
                outcomes.Count(o => o.DirectionHit) / (double)outcomes.Count,
                outcomes.Count);
        }

        string userPrompt = RecommendationPrompt.BuildUserPrompt(
            profile, run.Predictions, priorHoldings, trackRecord, language);

        // Regenerate on malformed JSON OR on context-pack violations (invented
        // tickers, broken allocations, risk-rule breaches); fail closed after
        // MaxParseAttempts — nothing unverified reaches the user (§ 3.6).
        LlmRecommendationResult? parsed = null;
        for (int attempt = 1; attempt <= MaxParseAttempts && parsed is null; attempt++)
        {
            string raw;
            try
            {
                raw = await llmClient.CompleteAsync(RecommendationPrompt.System, userPrompt, RecommendationPrompt.ResponseSchema, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LLM call failed for user {UserId}", request.UserId);
                return Result.Fail(LlmUnavailable);
            }

            parsed = TryParse(raw);
            if (parsed is null)
            {
                logger.LogWarning("LLM output failed to parse (attempt {Attempt}/{Max}) for user {UserId}",
                    attempt, MaxParseAttempts, request.UserId);
                continue;
            }

            IReadOnlyList<string> violations = LlmResponseValidator.Validate(
                parsed, run.Predictions, priorHoldings, profile.RiskProfile);
            if (violations.Count > 0)
            {
                logger.LogWarning(
                    "LLM output failed validation (attempt {Attempt}/{Max}) for user {UserId}: {Violations}",
                    attempt, MaxParseAttempts, request.UserId, string.Join(" | ", violations));
                parsed = null;
            }
        }

        if (parsed is null || parsed.Picks.Count == 0)
        {
            return Result.Fail(LlmUnavailable);
        }

        var response = new RecommendationResponse(parsed.Summary, parsed.Picks, run.GeneratedAt);
        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(24), cancellationToken);

        // Persist this run's BUY/HOLD picks as the user's holdings for the next run.
        var newHoldings = parsed.Picks
            .Where(p => p.AllocationPct > 0 && !string.Equals(p.Action, "SELL", StringComparison.OrdinalIgnoreCase))
            .Select(p => UserHolding.Create(request.UserId, p.Ticker, p.AllocationPct, run.GeneratedAt))
            .ToList();
        await holdingRepository.ReplaceForUserAsync(request.UserId, newHoldings, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(response);
    }

    private static LlmRecommendationResult? TryParse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string s = raw.Trim();

        // Strip markdown code fences if the model wrapped the JSON.
        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewline = s.IndexOf('\n');
            if (firstNewline >= 0)
            {
                s = s[(firstNewline + 1)..];
            }
            if (s.EndsWith("```", StringComparison.Ordinal))
            {
                s = s[..^3];
            }
            s = s.Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<LlmRecommendationResult>(s, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
