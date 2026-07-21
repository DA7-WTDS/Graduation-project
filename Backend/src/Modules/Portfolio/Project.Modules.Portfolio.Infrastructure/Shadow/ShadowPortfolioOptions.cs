namespace Project.Modules.Portfolio.Infrastructure.Shadow;

public sealed class ShadowPortfolioOptions
{
    /// <summary>Which market's templates + registry prices to run against.</summary>
    public string Market { get; set; } = "us";

    /// <summary>Fixed notional every model portfolio is run at (§ 6.1, e.g. 100k).</summary>
    public decimal Notional { get; set; } = 100_000m;

    /// <summary>One-side transaction cost, backtester parity (§ 1.8: 25 bps).</summary>
    public double CostPerSide { get; set; } = 0.0025;

    /// <summary>Nightly, 03:45 UTC — after the 03:30 valuation, which is after the
    /// 03:00 registry stats refresh, so the closes read are the same night's.</summary>
    public string CronSchedule { get; set; } = "0 45 3 ? * *";
}
