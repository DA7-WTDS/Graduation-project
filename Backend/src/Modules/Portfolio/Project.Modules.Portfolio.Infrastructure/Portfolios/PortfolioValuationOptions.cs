namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

public sealed class PortfolioValuationOptions
{
    /// <summary>Which market's registry prices to value against.</summary>
    public string Market { get; set; } = "us";

    /// <summary>Nightly, 03:30 UTC — after the 03:00 registry stats refresh, so
    /// the closes the valuation reads are the same night's.</summary>
    public string CronSchedule { get; set; } = "0 30 3 ? * *";

    /// <summary>Allocation drift that triggers a rebalance nudge (0.10 = 10
    /// percentage points on any single position vs its target).</summary>
    public double DriftThreshold { get; set; } = 0.10;
}
