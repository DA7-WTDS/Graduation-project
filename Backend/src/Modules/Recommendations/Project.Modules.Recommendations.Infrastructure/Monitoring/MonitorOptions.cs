namespace Project.Modules.Recommendations.Infrastructure.Monitoring;

public sealed class MonitorOptions
{
    /// <summary>Nightly, 03:15 UTC — after the 01:00 pipeline ingest so the
    /// latest run and fresh closes are both available.</summary>
    public string CronSchedule { get; set; } = "0 15 3 ? * *";

    /// <summary>Benchmark index for the crash trigger. ^GSPC for the US
    /// instance; the EGX instance will watch EGX30 when licensed data lands.</summary>
    public string IndexTicker { get; set; } = "^GSPC";

    /// <summary>Trailing trading-day window for the crash check.</summary>
    public int CrashWindowDays { get; set; } = 5;

    /// <summary>Drop that counts as a crash (0.05 = −5% over the window).</summary>
    public double CrashDropPct { get; set; } = 0.05;
}
