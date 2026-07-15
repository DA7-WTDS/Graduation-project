using Microsoft.EntityFrameworkCore;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.PublicApi;

namespace Project.Modules.Portfolio.Infrastructure.PublicApi;

internal sealed class PortfolioApi(PortfolioDbContext dbContext) : IPortfolioApi
{
    public async Task<MonitoringProfileResponse?> GetMonitoringProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // The user's newest scored profile across their goals (v1 UI is
        // single-goal, but take the latest either way).
        var profile = await (
            from g in dbContext.Goals
            join ip in dbContext.InvestorProfiles on g.Id equals ip.GoalId
            where g.UserId == userId
            orderby ip.CreatedAt descending
            select new { GoalType = g.Type, ip.RiskBand, ip.Engagement })
            .FirstOrDefaultAsync(cancellationToken);

        return profile is null
            ? null
            : new MonitoringProfileResponse(
                userId,
                profile.RiskBand.ToString(),
                profile.GoalType.ToString(),
                profile.Engagement.ToString());
    }

    public async Task<IReadOnlyList<Guid>> GetProfiledUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await (
            from g in dbContext.Goals
            join ip in dbContext.InvestorProfiles on g.Id equals ip.GoalId
            select g.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
