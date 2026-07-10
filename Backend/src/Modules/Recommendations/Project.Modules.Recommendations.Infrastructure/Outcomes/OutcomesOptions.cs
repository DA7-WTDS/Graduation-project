namespace Project.Modules.Recommendations.Infrastructure.Outcomes;

public sealed class OutcomesOptions
{
    /// <summary>Prediction horizon in calendar days (matches the model's 30-day target).</summary>
    public int HorizonDays { get; set; } = 30;

    /// <summary>Max predictions scored per job run (keeps the closes request bounded).</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Quartz cron for the nightly outcome scorer.
    /// Default: 02:30 UTC daily — after the 01:00 UTC pipeline run window.
    /// </summary>
    public string CronSchedule { get; set; } = "0 30 2 ? * *";
}
