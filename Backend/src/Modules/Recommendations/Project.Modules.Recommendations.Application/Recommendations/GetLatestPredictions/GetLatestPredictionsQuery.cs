using Project.Common.Application.Messaging;

namespace Project.Modules.Recommendations.Application.Recommendations.GetLatestPredictions;

public sealed record GetLatestPredictionsQuery : IQuery<PredictionsResponse>;
