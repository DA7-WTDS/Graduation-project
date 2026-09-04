namespace Project.Modules.Recommendations.Infrastructure.Pipeline;

public sealed class PipelineOptions
{
    /// <summary>Base URL of the Python pipeline service (default: http://localhost:8000).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// <summary>
    /// HTTP timeout in seconds for the /api/score call.
    ///
    /// The run is dominated by vendor rate-limit sleeps, not compute. Since sentiment is
    /// gathered for the WHOLE universe (MVP_PLAN § B), the floor is arithmetic: ~100 tickers
    /// × 3 Finnhub calls × the 1.05s throttle — 315s before a single headline is classified,
    /// a batch downloaded, or a tree evaluated. The old 300s default was already below that.
    ///
    /// 30 minutes gives real headroom. Nothing waits on this: the job is nightly and
    /// [DisallowConcurrentExecution], so a long run costs nothing but a held connection,
    /// while a short timeout costs the whole day's run AND its panel row.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 1800;

    /// <summary>
    /// Quartz cron expression for the daily pipeline trigger.
    /// Default: 01:00 UTC, Tuesday–Saturday (captures each Mon–Fri US session ~2h after close).
    /// Quartz format: seconds minutes hours day-of-month month day-of-week [year]
    /// </summary>
    public string CronSchedule { get; set; } = "0 0 1 ? * TUE,WED,THU,FRI,SAT";
}
