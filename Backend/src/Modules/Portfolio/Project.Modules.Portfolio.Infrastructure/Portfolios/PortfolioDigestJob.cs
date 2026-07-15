using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Domain.Goals;
using Project.Modules.Portfolio.Domain.Portfolios;
using Project.Modules.Portfolio.Domain.Proposals;
using Project.Modules.Portfolio.IntegrationEvents;
using Quartz;

namespace Project.Modules.Portfolio.Infrastructure.Portfolios;

/// <summary>
/// The periodic digest (§ 3.5, last row). Runs daily but sends nothing unless a
/// portfolio's own cadence has elapsed — set-and-forget investors hear from us
/// quarterly, everyone else monthly. Figures come from the aggregate (last
/// nightly valuation), so a digest always reports the same numbers the alerts
/// were based on.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class PortfolioDigestJob(
    IGoalPortfolioRepository portfolioRepository,
    IGoalRepository goalRepository,
    IPortfolioProposalRepository proposalRepository,
    IEventBus eventBus,
    IUnitOfWork unitOfWork,
    ILogger<PortfolioDigestJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        DateTime now = DateTime.UtcNow;

        IReadOnlyList<GoalPortfolio> portfolios = await portfolioRepository.GetAllActiveAsync(ct);
        if (portfolios.Count == 0)
        {
            logger.LogInformation("PortfolioDigestJob — no active portfolios.");
            return;
        }

        int sent = 0;

        foreach (GoalPortfolio portfolio in portfolios)
        {
            InvestorProfile? profile = await goalRepository.GetLatestProfileAsync(portfolio.GoalId, ct);
            string engagement = profile?.Engagement.ToString() ?? "Monthly";

            if (!DigestSchedule.IsDue(engagement, portfolio.InceptionDate, portfolio.LastDigestAt, now))
            {
                continue;
            }

            PortfolioProposal? proposal = await proposalRepository.GetByIdAsync(portfolio.ProposalId, ct);
            string cadence = proposal?.RebalanceCadence ?? "monthly";

            double totalReturn = portfolio.Amount > 0 ? portfolio.LastNav / (double)portfolio.Amount - 1 : 0;
            double drawdown = PortfolioValuation.Drawdown(portfolio.LastNav, portfolio.HighWaterMarkNav);

            await eventBus.PublishAsync(new PortfolioDigestDueIntegrationEvent(
                Guid.NewGuid(),
                now,
                portfolio.UserId,
                portfolio.GoalId,
                proposal?.TemplateName ?? "your plan",
                engagement,
                DigestSchedule.CadenceDays(engagement),
                portfolio.LastNav,
                totalReturn,
                drawdown,
                ReviewSchedule.NextReview(portfolio.InceptionDate, cadence, now)), ct);

            portfolio.MarkDigestSent(now);
            sent++;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PortfolioDigestJob — {Sent} digest(s) due of {Total} active portfolio(s).",
            sent, portfolios.Count);
    }
}
