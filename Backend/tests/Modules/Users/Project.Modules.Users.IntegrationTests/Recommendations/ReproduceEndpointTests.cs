using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Recommendations.Application.Abstractions.Pipeline;
using Project.Modules.Recommendations.Application.DailyRuns.Ingest;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Recommendations;

// Covers § 6.3: the feature snapshot survives ingest into jsonb intact, and the
// reproduce endpoint replays it and reports a verdict. The pipeline itself is
// stubbed — the Python inference core is verified separately in Pipeline tests;
// what matters here is that the snapshot round-trips through storage unchanged.
public sealed class ReproduceEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string Snapshot =
        """{"v":1,"lstm_window":[[0.11,0.22,0.33,0.44,0.55]],"tech_last":[0.66,0.77]}""";

    [Fact]
    public async Task A_feature_snapshot_survives_ingest_into_jsonb_intact()
    {
        await IngestAsync(DateTime.UtcNow, withSnapshot: true);

        using IServiceScope scope = Factory.Services.CreateScope();
        RecommendationsDbContext db = scope.ServiceProvider.GetRequiredService<RecommendationsDbContext>();
        StockPrediction stored = await db.Set<StockPrediction>().AsNoTracking().FirstAsync();

        stored.IsReproducible.Should().BeTrue();
        stored.ModelVersion.Should().Be(FakePipelineReproducer.CurrentModelVersion);
        stored.ScalerHash.Should().Be(FakePipelineReproducer.CurrentScalerHash);

        // jsonb normalises whitespace but must preserve values exactly — a lossy
        // round-trip here would silently break every future audit.
        using JsonDocument round = JsonDocument.Parse(stored.FeaturesJson!);
        round.RootElement.GetProperty("v").GetInt32().Should().Be(1);
        round.RootElement.GetProperty("lstm_window")[0][0].GetDouble().Should().Be(0.11);
        round.RootElement.GetProperty("tech_last")[1].GetDouble().Should().Be(0.77);
    }

    [Fact]
    public async Task Reproducing_a_snapshotted_prediction_reports_a_match()
    {
        await IngestAsync(DateTime.UtcNow, withSnapshot: true);
        Guid predictionId = await FirstPredictionIdAsync();

        HttpResponseMessage response = await ReproduceAsync(predictionId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("matches").GetBoolean().Should().BeTrue();
        body.GetProperty("ticker").GetString().Should().Be("AAPL");
        body.GetProperty("stored").GetProperty("modelVersion").GetString()
            .Should().Be(FakePipelineReproducer.CurrentModelVersion);
        body.GetProperty("modelVersionMatches").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Reproducing_a_prediction_that_predates_snapshotting_is_rejected_clearly()
    {
        await IngestAsync(DateTime.UtcNow, withSnapshot: false);
        Guid predictionId = await FirstPredictionIdAsync();

        HttpResponseMessage response = await ReproduceAsync(predictionId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("no stored feature snapshot");
    }

    [Fact]
    public async Task Reproduce_requires_the_pipeline_key()
    {
        await IngestAsync(DateTime.UtcNow, withSnapshot: true);
        Guid predictionId = await FirstPredictionIdAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/internal/predictions/{predictionId}/reproduce");
        request.Headers.Add("X-Pipeline-Key", "not-the-key");

        (await Client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reproducing_an_unknown_prediction_is_a_404()
    {
        (await ReproduceAsync(Guid.NewGuid())).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> FirstPredictionIdAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        RecommendationsDbContext db = scope.ServiceProvider.GetRequiredService<RecommendationsDbContext>();
        return await db.Set<StockPrediction>().AsNoTracking().Select(p => p.Id).FirstAsync();
    }

    private async Task<HttpResponseMessage> ReproduceAsync(Guid predictionId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/internal/predictions/{predictionId}/reproduce");
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);
        return await Client.SendAsync(request);
    }

    private async Task IngestAsync(DateTime generatedAt, bool withSnapshot)
    {
        var record = new PredictionRecordDto
        {
            Ticker = "AAPL",
            Direction = "UP",
            ChangePct = 2.5,
            Confidence = 0.9,
            SentimentScore = 0.4,
            Signal = "POSITIVE",
            Agreement = "CONFIRMED",
            RiskLevel = "LOW",
            ConvictionScore = 0.85,
            RiskFlags = ["signal_confirmed"],
            Rationale = "UP +2.5%",
        };

        if (withSnapshot)
        {
            // Serialize the DTO itself so its snake_case JsonPropertyName mapping
            // is exercised — an anonymous copy would silently bypass it.
            record = record with
            {
                Features = JsonSerializer.Deserialize<JsonElement>(Snapshot),
                ModelVersion = FakePipelineReproducer.CurrentModelVersion,
                ScalerHash = FakePipelineReproducer.CurrentScalerHash,
            };
        }

        var payload = new { generated_at = generatedAt, count = 1, records = new[] { record } };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/daily-results")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Pipeline-Key", IntegrationTestWebAppFactory.IngestApiKey);

        (await Client.SendAsync(request)).EnsureSuccessStatusCode();
    }
}
