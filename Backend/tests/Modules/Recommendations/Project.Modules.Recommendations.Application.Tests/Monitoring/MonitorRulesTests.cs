using FluentAssertions;
using Project.Modules.Recommendations.Domain.Monitoring;
using System;
using System.Collections.Generic;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.Monitoring;

// § 3.5 trigger rules: fire on crossing, alert once per flip — the difference
// between a monitoring engine and a spam machine.
public class MonitorRulesTests
{
    [Fact]
    public void Crash_fires_on_the_night_the_threshold_is_crossed()
    {
        // Flat at 100, then a plunge to 94 on the last day: 5-day window
        // yesterday = 0%, today = -6%.
        List<double> closes = [100, 100, 100, 100, 100, 100, 100, 94];

        (bool crossed, double drop) = MonitorRules.CrashCrossed(closes, windowDays: 5, dropPct: 0.05);

        crossed.Should().BeTrue();
        drop.Should().BeApproximately(-0.06, 1e-9);
    }

    [Fact]
    public void A_persisting_crash_does_not_refire_the_next_night()
    {
        // The plunge happened two days ago; both windows are already below.
        List<double> closes = [100, 100, 100, 100, 100, 100, 94, 93.9];

        (bool crossed, _) = MonitorRules.CrashCrossed(closes, windowDays: 5, dropPct: 0.05);

        crossed.Should().BeFalse();
    }

    [Fact]
    public void Recovery_and_a_second_plunge_fires_again()
    {
        // Crash, recovery back above threshold, then a fresh crossing.
        List<double> closes = [100, 94, 99, 100, 100, 100, 100, 100, 100, 93];

        (bool crossed, _) = MonitorRules.CrashCrossed(closes, windowDays: 5, dropPct: 0.05);

        crossed.Should().BeTrue();
    }

    [Fact]
    public void A_normal_pullback_never_fires()
    {
        List<double> closes = [100, 99.5, 99, 98.5, 98.2, 98, 97.8, 97.9];

        (bool crossed, _) = MonitorRules.CrashCrossed(closes, windowDays: 5, dropPct: 0.05);

        crossed.Should().BeFalse();
    }

    [Fact]
    public void Insufficient_history_is_never_a_crash()
    {
        (bool crossed, _) = MonitorRules.CrashCrossed([100, 94], windowDays: 5, dropPct: 0.05);

        crossed.Should().BeFalse();
    }

    private static Dictionary<string, (string, string)> Run(params (string Ticker, string Direction, string Signal)[] rows)
    {
        var d = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            d[r.Ticker] = (r.Direction, r.Signal);
        }

        return d;
    }

    [Fact]
    public void A_newly_flipped_holding_is_reported_once()
    {
        var latest = Run(("AAPL", "DOWN", "NEGATIVE"), ("MSFT", "UP", "POSITIVE"));
        var previous = Run(("AAPL", "UP", "NEUTRAL"), ("MSFT", "UP", "POSITIVE"));

        MonitorRules.NewReversals(["AAPL", "MSFT"], latest, previous)
            .Should().BeEquivalentTo(["AAPL"]);

        // Next night the flip persists — no new alert.
        MonitorRules.NewReversals(["AAPL", "MSFT"], latest, latest)
            .Should().BeEmpty();
    }

    [Fact]
    public void Model_down_alone_or_bad_news_alone_is_not_a_reversal()
    {
        var latest = Run(("AAA", "DOWN", "NEUTRAL"), ("BBB", "UP", "NEGATIVE"));
        var previous = Run(("AAA", "UP", "POSITIVE"), ("BBB", "UP", "POSITIVE"));

        MonitorRules.NewReversals(["AAA", "BBB"], latest, previous).Should().BeEmpty();
    }

    [Fact]
    public void A_ticker_absent_from_the_previous_run_counts_as_newly_flipped()
    {
        var latest = Run(("NEWX", "DOWN", "NEGATIVE"));

        MonitorRules.NewReversals(["NEWX"], latest, Run()).Should().BeEquivalentTo(["NEWX"]);
    }

    [Fact]
    public void Only_held_tickers_are_considered()
    {
        var latest = Run(("AAPL", "DOWN", "NEGATIVE"), ("TSLA", "DOWN", "NEGATIVE"));
        var previous = Run(("AAPL", "UP", "POSITIVE"), ("TSLA", "UP", "POSITIVE"));

        MonitorRules.NewReversals(["AAPL"], latest, previous).Should().BeEquivalentTo(["AAPL"]);
    }
}
