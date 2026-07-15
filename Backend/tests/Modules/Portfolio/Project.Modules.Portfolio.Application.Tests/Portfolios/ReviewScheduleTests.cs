using FluentAssertions;
using Project.Modules.Portfolio.Domain.Portfolios;
using System;
using System.Globalization;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios;

// § 3.2 cadences: the next review is always in the future, and a set-and-forget
// investor is never asked to look more often than their template says.
public class ReviewScheduleTests
{
    private static readonly DateTime Inception = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("weekly", "2026-01-22")]
    [InlineData("monthly", "2026-02-15")]
    [InlineData("quarterly", "2026-04-15")]
    [InlineData("semi_annual", "2026-07-15")]
    [InlineData("annual", "2027-01-15")]
    public void The_first_review_is_one_cadence_after_inception(string cadence, string expected)
    {
        DateTime now = Inception.AddDays(1);
        DateTime expectedUtc = DateTime.Parse(
            expected, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        ReviewSchedule.NextReview(Inception, cadence, now).Should().Be(expectedUtc);
    }

    [Fact]
    public void Past_occurrences_are_skipped_so_the_date_is_always_ahead()
    {
        // Held for ~5 months on a monthly cadence.
        DateTime now = Inception.AddMonths(5).AddDays(3);

        DateTime next = ReviewSchedule.NextReview(Inception, "monthly", now);

        next.Should().BeAfter(now);
        next.Should().Be(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void A_semi_annual_plan_is_not_asked_to_review_monthly()
    {
        DateTime now = Inception.AddMonths(2);

        ReviewSchedule.NextReview(Inception, "semi_annual", now)
            .Should().Be(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void An_unknown_cadence_falls_back_to_monthly()
    {
        ReviewSchedule.NextReview(Inception, "whenever", Inception.AddDays(1))
            .Should().Be(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc));
    }
}
