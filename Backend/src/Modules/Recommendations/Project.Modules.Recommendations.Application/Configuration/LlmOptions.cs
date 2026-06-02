namespace Project.Modules.Recommendations.Application.Configuration;

public sealed class LlmOptions
{
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";
    public string Model { get; set; } = "gemini-2.5-flash";

    /// <summary>Set via environment/user-secrets, never appsettings.</summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 4;
}
