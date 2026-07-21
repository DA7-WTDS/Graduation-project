using System.Collections.Generic;
using FluentAssertions;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Shadow;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Shadow;

public class ShadowPerformanceTests
{
    [Fact]
    public void Total_return_is_last_over_notional()
    {
        var series = new List<double> { 101_000, 103_000, 110_000 };
        ShadowPerformance.Summary s = ShadowPerformance.Compute(series, 100_000m);

        s.TotalReturn.Should().BeApproximately(0.10, 1e-9);
        s.Days.Should().Be(3);
    }

    [Fact]
    public void Max_drawdown_is_seeded_at_the_starting_notional()
    {
        // Immediate drop below the notional before any peak above it is still a drawdown.
        var series = new List<double> { 90_000, 95_000 };
        ShadowPerformance.Summary s = ShadowPerformance.Compute(series, 100_000m);

        s.MaxDrawdown.Should().BeApproximately(0.10, 1e-9);
    }

    [Fact]
    public void Max_drawdown_is_peak_to_trough()
    {
        var series = new List<double> { 100_000, 120_000, 90_000, 110_000 };
        ShadowPerformance.Summary s = ShadowPerformance.Compute(series, 100_000m);

        // 120k peak → 90k trough = 25%.
        s.MaxDrawdown.Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void Short_series_reports_raw_return_not_an_extrapolated_cagr()
    {
        var series = new List<double> { 105_000 };
        ShadowPerformance.Summary s = ShadowPerformance.Compute(series, 100_000m);

        // < 252 days: don't annualize 5% over one day into nonsense.
        s.AnnualizedReturn.Should().BeApproximately(s.TotalReturn, 1e-9);
    }

    [Fact]
    public void Empty_series_is_all_zero()
    {
        ShadowPerformance.Summary s = ShadowPerformance.Compute([], 100_000m);

        s.TotalReturn.Should().Be(0);
        s.MaxDrawdown.Should().Be(0);
        s.Days.Should().Be(0);
    }

    [Theory]
    [InlineData(20, 20, RiskProfile.Conservative)]
    [InlineData(40, 69, RiskProfile.Moderate)]
    [InlineData(70, 100, RiskProfile.Aggressive)]
    public void Risk_band_maps_from_template_range_midpoint(int min, int max, RiskProfile expected)
    {
        ShadowRiskBand.ForTemplate(min, max).Should().Be(expected);
    }
}
