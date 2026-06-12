namespace Project.Modules.Recommendations.Infrastructure.Pipeline;

public sealed class PipelineOptions
{
    /// <summary>Base URL of the Python pipeline service (default: http://localhost:8000).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// HTTP timeout in seconds for the /api/score call.
    /// Generous default (300s = 5 min) to cover the full predict + sentiment run for ~100 tickers.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Quartz cron expression for the daily pipeline trigger.
    /// Default: 01:00 UTC, Tuesday–Saturday (captures each Mon–Fri US session ~2h after close).
    /// Quartz format: seconds minutes hours day-of-month month day-of-week [year]
    /// </summary>
    public string CronSchedule { get; set; } = "0 0 1 ? * TUE,WED,THU,FRI,SAT";
}
