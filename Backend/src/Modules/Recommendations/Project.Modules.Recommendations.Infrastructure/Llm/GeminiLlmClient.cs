using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.Configuration;

namespace Project.Modules.Recommendations.Infrastructure.Llm;

/// <summary>
/// Google Gemini client using the NATIVE generateContent endpoint with responseSchema,
/// which guarantees schema-valid JSON output (no malformed-JSON parse failures).
/// Auth via the x-goog-api-key header. Retries transient 429/5xx/timeout with backoff.
/// </summary>
internal sealed class GeminiLlmClient(
    HttpClient httpClient,
    IOptions<LlmOptions> options,
    ILogger<GeminiLlmClient> logger) : ILlmClient
{
    private readonly LlmOptions _options = options.Value;

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string? jsonSchema = null,
        CancellationToken cancellationToken = default)
    {
        var generationConfig = new Dictionary<string, object?> { ["temperature"] = 0.3 };
        if (!string.IsNullOrWhiteSpace(jsonSchema))
        {
            generationConfig["responseMimeType"] = "application/json";
            generationConfig["responseSchema"] = JsonNode.Parse(jsonSchema);
        }

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
            generationConfig,
        };

        string url = $"v1beta/models/{_options.Model}:generateContent";

        int attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, payload, cancellationToken);

                int status = (int)response.StatusCode;
                if ((status == 429 || status >= 500) && attempt <= _options.MaxRetries)
                {
                    int delayMs = 800 * attempt;
                    logger.LogWarning("Gemini transient {Status}; retry {Attempt}/{Max} in {Delay}ms",
                        status, attempt, _options.MaxRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument doc = JsonDocument.Parse(body);

                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;
            }
            catch (HttpRequestException ex) when (attempt <= _options.MaxRetries)
            {
                logger.LogWarning(ex, "Gemini request error; retry {Attempt}/{Max}", attempt, _options.MaxRetries);
                await Task.Delay(800 * attempt, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt <= _options.MaxRetries)
            {
                logger.LogWarning(ex, "Gemini timeout; retry {Attempt}/{Max}", attempt, _options.MaxRetries);
                await Task.Delay(800 * attempt, cancellationToken);
            }
        }
    }
}
