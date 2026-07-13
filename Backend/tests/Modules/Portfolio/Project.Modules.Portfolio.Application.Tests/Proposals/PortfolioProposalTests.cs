using FluentAssertions;
using Project.Modules.Portfolio.Domain.Allocation;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Proposals;
using System;
using System.Collections.Generic;
using Xunit;

namespace Project.Modules.Portfolio.Application.Tests.Proposals;

// The proposal is an audit record: accepting must never change the numbers, and
// the status machine (Proposed → Accepted → Superseded) must be one-directional.
public class PortfolioProposalTests
{
    private static PortfolioProposal New(int version = 1) => PortfolioProposal.Create(
        Guid.NewGuid(), version, "balanced_growth", "Balanced Growth", "monthly", 0.12,
        RiskProfile.Moderate, 55, 10_000m,
        [new AllocationPosition("SPY", "core", 0.5, 5000m, "rank #1")],
        ["some assumption"],
        "abc123");

    [Fact]
    public void A_new_proposal_starts_proposed_and_round_trips_its_positions()
    {
        PortfolioProposal p = New();

        p.Status.Should().Be(ProposalStatus.Proposed);
        p.AcceptedAt.Should().BeNull();
        p.GetPositions().Should().ContainSingle(pos => pos.Symbol == "SPY" && pos.Weight == 0.5);
        p.GetAssumptions().Should().ContainSingle(a => a == "some assumption");
    }

    [Fact]
    public void Accepting_flips_status_and_stamps_the_time()
    {
        PortfolioProposal p = New();

        p.Accept().Should().BeTrue();
        p.Status.Should().Be(ProposalStatus.Accepted);
        p.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public void Re_accepting_is_an_idempotent_no_op()
    {
        PortfolioProposal p = New();
        p.Accept();
        DateTime? firstAcceptedAt = p.AcceptedAt;

        p.Accept().Should().BeFalse();
        p.AcceptedAt.Should().Be(firstAcceptedAt);
        p.Status.Should().Be(ProposalStatus.Accepted);
    }

    [Fact]
    public void Superseding_only_affects_an_accepted_proposal()
    {
        PortfolioProposal proposed = New();
        proposed.Supersede();
        proposed.Status.Should().Be(ProposalStatus.Proposed); // no-op on a non-accepted one

        PortfolioProposal accepted = New();
        accepted.Accept();
        accepted.Supersede();
        accepted.Status.Should().Be(ProposalStatus.Superseded);
    }

    [Fact]
    public void A_superseded_proposal_can_never_be_accepted_again()
    {
        PortfolioProposal p = New();
        p.Accept();
        p.Supersede();

        Action act = () => p.Accept();

        act.Should().Throw<InvalidOperationException>();
    }
}
