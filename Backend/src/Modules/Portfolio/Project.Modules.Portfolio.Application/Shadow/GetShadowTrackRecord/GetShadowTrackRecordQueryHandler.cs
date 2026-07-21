using FluentResults;
using Microsoft.Extensions.Options;
using Project.Common.Application.Messaging;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;
using Project.Modules.Portfolio.Domain.Shadow;

namespace Project.Modules.Portfolio.Application.Shadow.GetShadowTrackRecord;

internal sealed class GetShadowTrackRecordQueryHandler(
    IShadowPortfolioRepository shadowRepository,
    IOptions<ShadowTrackRecordOptions> options)
    : IQueryHandler<GetShadowTrackRecordQuery, ShadowTrackRecordResponse>
{
    private const string Disclaimer =
        "Model portfolios, costs simulated. These are hypothetical results from running each " +
        "strategy as a paper portfolio — not the returns of any real client account. " +
        "Past performance does not guarantee future results. Informational only, not financial advice.";

    public async Task<Result<ShadowTrackRecordResponse>> Handle(
        GetShadowTrackRecordQuery request, CancellationToken cancellationToken)
    {
        string market = options.Value.Market;

        IReadOnlyList<ShadowPortfolio> portfolios = await shadowRepository.GetAllForMarketAsync(market, cancellationToken);
        IReadOnlyList<ShadowSnapshot> snapshots = await shadowRepository.GetAllSnapshotsAsync(market, cancellationToken);

        var snapshotsByPortfolio = snapshots
            .GroupBy(s => s.ShadowPortfolioId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).ToList());

        var series = new List<ShadowSeries>();
        foreach (ShadowPortfolio p in portfolios.OrderBy(p => p.TemplateKey, StringComparer.Ordinal))
        {
            List<ShadowSnapshot> points = snapshotsByPortfolio.GetValueOrDefault(p.Id, []);
            ShadowPerformance.Summary summary = ShadowPerformance.Compute(
                points.Select(s => s.Nav).ToList(), p.Notional);

            series.Add(new ShadowSeries(
                p.TemplateKey,
                p.TemplateName,
                p.RiskBand.ToString(),
                p.RebalanceCadence,
                p.Notional,
                p.InceptionDate,
                p.LastNav,
                summary.TotalReturn,
                summary.AnnualizedReturn,
                summary.MaxDrawdown,
                summary.Days,
                points.Select(s => new ShadowNavPoint(s.Date, s.Nav, s.DailyReturn, s.Rebalanced)).ToList()));
        }

        return Result.Ok(new ShadowTrackRecordResponse(Disclaimer, series));
    }
}

/// <summary>Which market's shadow portfolios the public read returns.</summary>
public sealed class ShadowTrackRecordOptions
{
    public string Market { get; set; } = "us";
}
