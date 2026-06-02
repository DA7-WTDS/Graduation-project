using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;

public sealed record GetRecommendationsQuery(Guid UserId) : IQuery<RecommendationResponse>;
