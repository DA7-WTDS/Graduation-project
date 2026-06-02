using Project.Common.Domain.Abstractions;

namespace Project.Modules.Recommendations.Domain.DailyRuns;

/// <summary>
/// A single daily pipeline run (the ~100-stock market-wide batch), aggregate root
/// over its <see cref="StockPrediction"/> children. Ingested from the n8n pipeline.
/// </summary>
public sealed class DailyRun : Entity
{
    private readonly List<StockPrediction> _predictions = [];

    private DailyRun() { }

    public Guid Id { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public int Count { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<StockPrediction> Predictions => _predictions.AsReadOnly();

    public static DailyRun Create(DateTime generatedAt, IEnumerable<StockPrediction> predictions)
    {
        var run = new DailyRun
        {
            Id = Guid.NewGuid(),
            GeneratedAt = generatedAt,
            CreatedAt = DateTime.UtcNow
        };

        run._predictions.AddRange(predictions);
        run.Count = run._predictions.Count;

        return run;
    }
}
