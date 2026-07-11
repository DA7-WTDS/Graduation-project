namespace Project.Modules.Portfolio.Infrastructure.Instruments;

public sealed class InstrumentsOptions
{
    /// <summary>Base URL of the Python pipeline service (source of computed stats).</summary>
    public string PipelineBaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>Timeout for /api/instrument-stats — a full-universe request
    /// downloads a year of OHLCV per ticker, so give it room.</summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Which market this instance registers equities for (mirrors the
    /// pipeline's MARKET env; two instances per D5 when EGX activates).</summary>
    public string Market { get; set; } = "us";

    /// <summary>Nightly, 03:00 UTC — after the 01:00 pipeline run and the 02:30
    /// outcome scorer, so the same data day flows through all three.</summary>
    public string CronSchedule { get; set; } = "0 0 3 ? * *";
}
