namespace Project.Modules.Recommendations.Application.Configuration;

public sealed class IngestOptions
{
    /// <summary>Shared secret the pipeline must send in the X-Pipeline-Key header. Set via env/user-secrets.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// § 6.2 kill switch: when true, clean runs land as PendingReview and an
    /// operator must publish each one. Intended for the early production months.
    /// </summary>
    public bool RequireManualApproval { get; set; }
}
