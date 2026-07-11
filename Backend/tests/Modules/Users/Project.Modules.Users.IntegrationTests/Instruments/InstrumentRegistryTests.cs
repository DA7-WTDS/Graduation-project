using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Instruments;
using Project.Modules.Users.IntegrationTests.Infrastructure;
using Quartz;

namespace Project.Modules.Users.IntegrationTests.Instruments;

// § 3.1 instrument registry: migration-seeded sleeve ETFs + the nightly refresh
// job (auto-registration of screened equities, stat upserts, no destructive
// behavior on vendor gaps). The pipeline is faked at the HttpMessageHandler
// level so the job's real HTTP + JSON path is exercised.
public sealed class InstrumentRegistryTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed class FakePipelineHandler(Func<string?, string> respond) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respond(body), Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string StatsJson(params (string Ticker, double? Vol, double? Adv, double? Close, string? Sector)[] stats)
    {
        var payload = new
        {
            market = "us",
            as_of = "2026-07-11T03:00:00Z",
            stats = stats.Select(s => new
            {
                ticker = s.Ticker,
                realized_vol_1y = s.Vol,
                avg_daily_value_traded = s.Adv,
                last_close = s.Close,
                sector = s.Sector,
            }),
        };
        return JsonSerializer.Serialize(payload);
    }

    private async Task RunJobAsync(Func<string?, string> respond)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var httpClient = new HttpClient(new FakePipelineHandler(respond)) { BaseAddress = new Uri("http://fake-pipeline") };
        var job = new RefreshInstrumentStatsJob(
            httpClient, db,
            Options.Create(new InstrumentsOptions { Market = "us" }),
            NullLogger<RefreshInstrumentStatsJob>.Instance);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        await job.Execute(context);
    }

    [Fact]
    public async Task Migration_seeds_the_curated_sleeve_etfs()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        List<Instrument> seeded = db.Set<Instrument>().Where(i => i.Type == InstrumentType.Etf).ToList();

        seeded.Select(i => i.Symbol).Should().BeEquivalentTo(["SPY", "GLD", "AGG", "BIL"]);
        seeded.Single(i => i.Symbol == "GLD").AssetClass.Should().Be(AssetClass.Gold);
        seeded.Single(i => i.Symbol == "GLD").SuitableFor.Should().Contain(Sleeves.Stability);
        seeded.Single(i => i.Symbol == "SPY").SuitableFor.Should().Contain(Sleeves.Core);
    }

    [Fact]
    public async Task Refresh_auto_registers_universe_equities_and_updates_etf_stats()
    {
        // Universe call returns two screened stocks; the follow-up registry call
        // (for the seeded ETFs the universe didn't cover) returns their stats.
        await RunJobAsync(body =>
            body is not null && body.Contains("SPY")
                ? StatsJson(("SPY", 0.15, 5e10, 620.0, null), ("GLD", 0.13, 2e9, 310.0, null),
                            ("AGG", 0.05, 1e9, 99.0, null), ("BIL", 0.002, 8e8, 91.5, null))
                : StatsJson(("AAPL", 0.24, 1.4e10, 315.0, "Technology"), ("MSFT", 0.21, 1.1e10, 560.0, "Technology")));

        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        List<Instrument> all = db.Set<Instrument>().ToList();

        // Screened equities auto-registered as core-sleeve stocks with stats.
        Instrument aapl = all.Single(i => i.Symbol == "AAPL");
        aapl.Type.Should().Be(InstrumentType.Stock);
        aapl.AssetClass.Should().Be(AssetClass.Equity);
        aapl.SuitableFor.Should().BeEquivalentTo([Sleeves.Core]);
        aapl.Sector.Should().Be("Technology");
        aapl.RealizedVol1Y.Should().BeApproximately(0.24, 1e-9);

        // Seeded ETFs got their stats from the follow-up call.
        all.Single(i => i.Symbol == "GLD").RealizedVol1Y.Should().BeApproximately(0.13, 1e-9);
        all.Single(i => i.Symbol == "BIL").LastClose.Should().BeApproximately(91.5, 1e-9);
    }

    [Fact]
    public async Task Rerunning_the_refresh_updates_in_place_without_duplicates()
    {
        await RunJobAsync(body =>
            body is not null && body.Contains("SPY")
                ? StatsJson(("SPY", 0.15, 5e10, 620.0, null), ("GLD", 0.13, 2e9, 310.0, null),
                            ("AGG", 0.05, 1e9, 99.0, null), ("BIL", 0.002, 8e8, 91.5, null))
                : StatsJson(("AAPL", 0.24, 1.4e10, 315.0, "Technology")));

        // Second night: AAPL vol moved; vendor returned no volume data (null ADV).
        await RunJobAsync(body =>
            body is not null && body.Contains("SPY")
                ? StatsJson(("SPY", 0.16, 5e10, 625.0, null), ("GLD", 0.14, 2e9, 312.0, null),
                            ("AGG", 0.05, 1e9, 99.1, null), ("BIL", 0.002, 8e8, 91.6, null))
                : StatsJson(("AAPL", 0.30, null, 320.0, "Technology")));

        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        List<Instrument> aapls = db.Set<Instrument>().Where(i => i.Symbol == "AAPL").ToList();

        aapls.Should().ContainSingle();
        aapls[0].RealizedVol1Y.Should().BeApproximately(0.30, 1e-9);
        // Null stat = vendor gap → previous value survives, never blanked.
        aapls[0].AvgDailyValueTraded.Should().BeApproximately(1.4e10, 1e-3);
    }
}
