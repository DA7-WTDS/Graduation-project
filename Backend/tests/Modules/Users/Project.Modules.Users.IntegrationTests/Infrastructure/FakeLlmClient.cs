using Project.Modules.Recommendations.Application.Abstractions.Llm;

namespace Project.Modules.Users.IntegrationTests.Infrastructure;

/// <summary>
/// Deterministic stand-in for the Gemini client so integration tests never call the
/// real LLM. Returns a fixed, schema-valid recommendation payload and counts how many
/// times it was invoked, which lets a test prove the 24-hour cache short-circuits the
/// second request.
/// </summary>
internal sealed class FakeLlmClient : ILlmClient
{
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string? jsonSchema = null,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromResult(CannedResponse);
    }

    private const string CannedResponse = """
    {
      "summary": "Test recommendations for the authenticated profile. Informational only, not financial advice.",
      "picks": [
        { "ticker": "AAPL", "action": "BUY", "allocation_pct": 60, "reason": "Confirmed upward signal with strong conviction.", "risk_note": "Low risk grade.", "fit": "Fits a moderate profile." },
        { "ticker": "MSFT", "action": "BUY", "allocation_pct": 40, "reason": "Positive sentiment confirms the price trend.", "risk_note": "Low risk grade.", "fit": "Fits a moderate profile." }
      ]
    }
    """;
}
