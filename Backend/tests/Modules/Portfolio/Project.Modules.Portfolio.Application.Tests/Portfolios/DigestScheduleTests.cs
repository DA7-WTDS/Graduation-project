using FluentAssertions;
using Project.Modules.Portfolio.Domain.Portfolios;
using System;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Portfolios;

// § 3.5: the digest cadence is the promise that answering "only tell me when it
// matters" means something — a set-and-forget investor must not get monthly mail.
public class DigestScheduleTests
{
    private static readonly DateTime Inception = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("SetAndForget", 90)]
    [InlineData("Monthly", 30)]
    [InlineData("Daily", 30)]
    [InlineData(null, 30)]
    public void Cadence_follows_the_engagement_answer(string? engagement, int expectedDays)
    {
        DigestSchedule.CadenceDays(engagement).Should().Be(expectedDays);
    }

    [Fact]
    public void A_brand_new_portfolio_is_not_summarized_on_day_one()
    {
        DigestSchedule.IsDue("Monthly", Inception, null, Inception.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void The_first_digest_lands_one_cadence_after_inception()
    {
        DigestSchedule.IsDue("Monthly", Inception, null, Inception.AddDays(29)).Should().BeFalse();
        DigestSchedule.IsDue("Monthly", Inception, null, Inception.AddDays(30)).Should().BeTrue();
    }

    [Fact]
    public void Sending_a_digest_rearms_the_clock()
    {
        DateTime sentAt = Inception.AddDays(30);

        DigestSchedule.IsDue("Monthly", Inception, sentAt, sentAt.AddDays(5)).Should().BeFalse();
        DigestSchedule.IsDue("Monthly", Inception, sentAt, sentAt.AddDays(30)).Should().BeTrue();
    }

    [Fact]
    public void A_set_and_forget_investor_waits_a_quarter_not_a_month()
    {
        // Two months in — a monthly investor is overdue, set-and-forget is not.
        DateTime twoMonths = Inception.AddDays(60);

        DigestSchedule.IsDue("Monthly", Inception, null, twoMonths).Should().BeTrue();
        DigestSchedule.IsDue("SetAndForget", Inception, null, twoMonths).Should().BeFalse();
        DigestSchedule.IsDue("SetAndForget", Inception, null, Inception.AddDays(90)).Should().BeTrue();
    }
}
