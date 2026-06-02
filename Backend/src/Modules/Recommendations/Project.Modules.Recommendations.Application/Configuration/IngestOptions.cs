namespace Project.Modules.Recommendations.Application.Configuration;

public sealed class IngestOptions
{
    /// <summary>Shared secret the pipeline must send in the X-Pipeline-Key header. Set via env/user-secrets.</summary>
    public string? ApiKey { get; set; }
}
