using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

/// <summary>Language is "en" or "ar" (§ 3.6) — anything else falls back to English.</summary>
public sealed record GetRecommendationsQuery(Guid UserId, string Language = "en") : IQuery<RecommendationResponse>;
