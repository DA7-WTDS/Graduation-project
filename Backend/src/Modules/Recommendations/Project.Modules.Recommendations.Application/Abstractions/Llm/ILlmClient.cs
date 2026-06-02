namespace Project.Modules.Recommendations.Application.Abstractions.Llm;

public interface ILlmClient
{
    /// <summary>
    /// Sends a system + user prompt to the LLM and returns the model's raw text response.
    /// When <paramref name="jsonSchema"/> is provided (a JSON-schema string), the response is
    /// constrained to that schema (structured output) and returned as a JSON string.
    /// Implementations handle transient retries; throws only on unrecoverable failure.
    /// </summary>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string? jsonSchema = null,
        CancellationToken cancellationToken = default);
}
