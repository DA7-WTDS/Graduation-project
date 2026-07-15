using FluentAssertions;
using Project.Modules.Portfolio.PublicApi;
using Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;
using Project.Modules.Recommendations.Domain.DailyRuns;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Project.Modules.Recommendations.Application.Tests.Recommendations;

// § 3.6 live eval — the same principle as Pipeline/training/eval_sentiment_llm.py:
// every prompt or model change must pass this golden set against the REAL model.
// Skips gracefully (passes with a note) when no Gemini key is available, so CI
// without secrets stays green while local/keyed runs exercise the full path.
public class GeminiLiveEvalTests(ITestOutputHelper output)
{
    private const string Model = "gemini-2.5-flash";

    private static string? ApiKey()
    {
        string? key = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("Recommendations__Llm__ApiKey");
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        // Walk up from bin/ to find the repo-root .env (same trick as the Python eval).
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string envPath = Path.Combine(dir.FullName, ".env");
            if (!File.Exists(envPath))
            {
                continue;
            }

            foreach (string line in File.ReadAllLines(envPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("Recommendations__Llm__ApiKey=", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("GEMINI_API_KEY=", StringComparison.OrdinalIgnoreCase))
                {
                    string value = trimmed[(trimmed.IndexOf('=') + 1)..].Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static readonly List<StockPrediction> GoldenCandidates =
    [
        StockPrediction.Create("AAPL", "UP", 2.5, 0.9, 0.4, "POSITIVE", null, null, null, null, "CONFIRMED", "LOW", 0.85, ["signal_confirmed"], "strong"),
        StockPrediction.Create("MSFT", "UP", 1.8, 0.85, 0.3, "POSITIVE", null, null, null, null, "CONFIRMED", "LOW", 0.8, [], "steady"),
        StockPrediction.Create("NVDA", "UP", 3.1, 0.8, 0.2, "NEUTRAL", null, null, null, null, "NEUTRAL", "MEDIUM", 0.7, [], "momentum"),
        StockPrediction.Create("TSLA", "UP", 4.0, 0.6, -0.2, "NEGATIVE", null, null, null, null, "CONTRADICT", "HIGH", 0.4, ["signal_contradiction"], "volatile"),
        StockPrediction.Create("XOM", "DOWN", -1.2, 0.7, -0.1, "NEUTRAL", null, null, null, null, "NEUTRAL", "MEDIUM", 0.5, [], "weak"),
    ];

    private static MonitoringProfileResponse Profile(string risk) =>
        new(Guid.NewGuid(), risk, "LongTermWealth", "Monthly");

    private static async Task<string> CallGeminiAsync(string key, string userPrompt)
    {
        using var http = new HttpClient { BaseAddress = new Uri("https://generativelanguage.googleapis.com/") };
        http.DefaultRequestHeaders.Add("x-goog-api-key", key);
        http.Timeout = TimeSpan.FromSeconds(90);

        // Mirrors GeminiLlmClient's request exactly (native endpoint + responseSchema).
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = RecommendationPrompt.System } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
            generationConfig = new Dictionary<string, object?>
            {
                ["temperature"] = 0.3,
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = JsonNode.Parse(RecommendationPrompt.ResponseSchema),
            },
        };

        HttpResponseMessage response = await http.PostAsJsonAsync($"v1beta/models/{Model}:generateContent", payload);
        response.EnsureSuccessStatusCode();
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content")
            .GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private async Task<LlmRecommendationResult> RunEvalAsync(string key, string risk, string language)
    {
        string prompt = RecommendationPrompt.BuildUserPrompt(
            Profile(risk), GoldenCandidates, [],
            new TrackRecordSnippet(0.483, 201),
            language);

        string raw = await CallGeminiAsync(key, prompt);
        var parsed = JsonSerializer.Deserialize<LlmRecommendationResult>(
            raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        parsed.Should().NotBeNull("Gemini must return schema-valid JSON");

        IReadOnlyList<string> violations = LlmResponseValidator.Validate(parsed!, GoldenCandidates, [], risk);
        violations.Should().BeEmpty($"a real {risk}/{language} response must pass the context-pack validator");
        return parsed!;
    }

    [Fact]
    public async Task Live_english_response_is_grounded_and_valid()
    {
        string? key = ApiKey();
        if (key is null)
        {
            output.WriteLine("SKIPPED — no Gemini key in environment or repo .env.");
            return;
        }

        LlmRecommendationResult result = await RunEvalAsync(key, "Conservative", "en");

        // Conservative + validator already guarantee no TSLA; spot-check grounding.
        result.Picks.Select(p => p.Ticker).Should().NotContain("TSLA");
        result.Summary.ToLowerInvariant().Should().ContainAny("not financial advice", "informational");
        output.WriteLine($"EN OK — {result.Picks.Count} picks: {string.Join(", ", result.Picks.Select(p => $"{p.Ticker} {p.Action} {p.AllocationPct}%"))}");
    }

    [Fact]
    public async Task Live_arabic_response_is_grounded_valid_and_actually_arabic()
    {
        string? key = ApiKey();
        if (key is null)
        {
            output.WriteLine("SKIPPED — no Gemini key in environment or repo .env.");
            return;
        }

        LlmRecommendationResult result = await RunEvalAsync(key, "Moderate", "ar");

        result.Summary.Any(c => c is >= '؀' and <= 'ۿ')
            .Should().BeTrue("the summary must be written in Arabic script");
        result.Picks.Should().OnlyContain(p => p.Ticker.All(char.IsAscii), "tickers stay in English");
        output.WriteLine($"AR OK — summary: {result.Summary[..Math.Min(120, result.Summary.Length)]}");
    }
}
