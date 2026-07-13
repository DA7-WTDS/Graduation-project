using FluentAssertions;
using Project.Modules.Recommendations.Application.Recommendations.GetRecommendations;
using Project.Modules.Recommendations.Domain.DailyRuns;
using Project.Modules.Recommendations.Domain.Holdings;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Project.Modules.Recommendations.Application.Tests.Recommendations;

// § 3.6 eval harness, golden set: every hallucination class the validator must
// catch. These run in CI on every prompt/model change — the LLM is allowed to
// get smarter, never to get less grounded.
public class LlmResponseValidatorTests
{
    private static StockPrediction Candidate(string ticker, string risk = "LOW", string[]? flags = null) =>
        StockPrediction.Create(ticker, "UP", 2.0, 0.9, 0.3, "POSITIVE", null, null, null, null,
            "CONFIRMED", risk, 0.8, flags ?? [], "test");

    private static UserHolding Held(string ticker) =>
        UserHolding.Create(Guid.NewGuid(), ticker, 50, DateTime.UtcNow.AddDays(-1));

    private static RecommendationItem Pick(string ticker, string action, double alloc) =>
        new(ticker, action, alloc, "reason", "risk", "fit");

    private static LlmRecommendationResult Result(params RecommendationItem[] picks) =>
        new() { Summary = "s", Picks = picks.ToList() };

    private static readonly List<StockPrediction> Candidates =
        [Candidate("AAPL"), Candidate("MSFT"), Candidate("TSLA", risk: "HIGH"), Candidate("XOM", flags: ["signal_contradiction"])];

    [Fact]
    public void A_clean_response_passes()
    {
        var result = Result(Pick("AAPL", "BUY", 60), Pick("MSFT", "BUY", 40));

        LlmResponseValidator.Validate(result, Candidates, [], "Moderate").Should().BeEmpty();
    }

    [Fact]
    public void An_invented_ticker_is_rejected()
    {
        var result = Result(Pick("AAPL", "BUY", 50), Pick("HALLUCIN", "BUY", 50));

        LlmResponseValidator.Validate(result, Candidates, [], "Moderate")
            .Should().ContainSingle(v => v.Contains("HALLUCIN") && v.Contains("invented"));
    }

    [Fact]
    public void Buying_a_held_name_without_a_fresh_signal_is_rejected()
    {
        // OLDCO is held but absent from today's run — HOLD is fine, BUY is not.
        var buy = Result(Pick("OLDCO", "BUY", 100));
        var hold = Result(Pick("OLDCO", "HOLD", 100));

        LlmResponseValidator.Validate(buy, Candidates, [Held("OLDCO")], "Moderate")
            .Should().ContainSingle(v => v.Contains("without a fresh signal"));
        LlmResponseValidator.Validate(hold, Candidates, [Held("OLDCO")], "Moderate")
            .Should().BeEmpty();
    }

    [Fact]
    public void Allocations_must_sum_to_one_hundred()
    {
        var result = Result(Pick("AAPL", "BUY", 30), Pick("MSFT", "BUY", 30));

        LlmResponseValidator.Validate(result, Candidates, [], "Moderate")
            .Should().ContainSingle(v => v.Contains("sum to 60"));
    }

    [Fact]
    public void A_sell_must_carry_zero_allocation_and_not_count_toward_the_sum()
    {
        var valid = Result(Pick("AAPL", "BUY", 100), Pick("MSFT", "SELL", 0));
        var invalid = Result(Pick("AAPL", "BUY", 100), Pick("MSFT", "SELL", 20));

        LlmResponseValidator.Validate(valid, Candidates, [Held("MSFT")], "Moderate").Should().BeEmpty();
        LlmResponseValidator.Validate(invalid, Candidates, [Held("MSFT")], "Moderate")
            .Should().Contain(v => v.Contains("SELL must carry allocation 0"));
    }

    [Fact]
    public void High_risk_and_flagged_buys_are_blocked_for_conservative_users()
    {
        var highRisk = Result(Pick("AAPL", "BUY", 50), Pick("TSLA", "BUY", 50));
        var flagged = Result(Pick("AAPL", "BUY", 50), Pick("XOM", "BUY", 50));

        LlmResponseValidator.Validate(highRisk, Candidates, [], "Conservative")
            .Should().ContainSingle(v => v.Contains("HIGH-risk BUY"));
        LlmResponseValidator.Validate(flagged, Candidates, [], "Conservative")
            .Should().ContainSingle(v => v.Contains("flagged"));

        // The same picks are legal for an Aggressive profile.
        LlmResponseValidator.Validate(highRisk, Candidates, [], "Aggressive").Should().BeEmpty();
    }

    [Fact]
    public void Duplicates_bad_actions_and_out_of_range_allocations_are_rejected()
    {
        var duplicated = Result(Pick("AAPL", "BUY", 50), Pick("AAPL", "HOLD", 50));
        var badAction = Result(Pick("AAPL", "YOLO", 100));
        var outOfRange = Result(Pick("AAPL", "BUY", 130));

        LlmResponseValidator.Validate(duplicated, Candidates, [], "Moderate")
            .Should().Contain(v => v.Contains("duplicated"));
        LlmResponseValidator.Validate(badAction, Candidates, [], "Moderate")
            .Should().Contain(v => v.Contains("invalid action"));
        LlmResponseValidator.Validate(outOfRange, Candidates, [], "Moderate")
            .Should().Contain(v => v.Contains("outside 0-100"));
    }

    [Fact]
    public void An_empty_pick_list_is_rejected()
    {
        LlmResponseValidator.Validate(Result(), Candidates, [], "Moderate")
            .Should().ContainSingle(v => v.Contains("no picks"));
    }
}
