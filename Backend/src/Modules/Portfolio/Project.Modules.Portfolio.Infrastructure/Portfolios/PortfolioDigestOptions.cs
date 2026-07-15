namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

public sealed class PortfolioDigestOptions
{
    /// <summary>Checked daily at 07:00 UTC — the job runs every day but only the
    /// portfolios whose cadence has elapsed actually get a digest.</summary>
    public string CronSchedule { get; set; } = "0 0 7 ? * *";
}
