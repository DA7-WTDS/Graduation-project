using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;
using Project.Modules.Portfolio.Application.Abstractions.Strategies;
using Project.Modules.Portfolio.Domain.Instruments;
using Project.Modules.Portfolio.Domain.Shadow;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Shadow;
using Project.Modules.Recommendations.PublicApi;
using Project.Modules.Users.IntegrationTests.Infrastructure;
using Quartz;

namespace Project.Modules.Users.IntegrationTests.Monitoring;

// § 6.1: the nightly shadow job runs every template as a fixed-notional paper
// portfolio. Exercised through the retirement template (all-ETF: SPY/GLD/AGG/BIL),
// so it needs no ranked daily run — just the four sleeve prices, which live in the
// Respawn-ignored registry and are set up front for order-independence.
public sealed class ShadowPortfolioJobTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly string[] Etfs = ["SPY", "GLD", "AGG", "BIL"];
    private const string RetirementKey = "retirement_set_and_forget";

    private async Task SetPricesAsync(double price)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        var set = db.Set<Instrument>();
        foreach (string symbol in Etfs)
        {
            set.FirstOrDefault(x => x.Symbol == symbol)?.UpdateStats(0.12, 5_000_000, price, null, DateTime.UtcNow);
        }
        await db.SaveChangesAsync();
    }

    private async Task RunShadowJobAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        var job = new ShadowPortfolioJob(
            scope.ServiceProvider.GetRequiredService<IShadowPortfolioRepository>(),
            scope.ServiceProvider.GetRequiredService<IStrategyTemplateRepository>(),
            scope.ServiceProvider.GetRequiredService<IInstrumentRepository>(),
            scope.ServiceProvider.GetRequiredService<IRecommendationsApi>(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Options.Create(new ShadowPortfolioOptions { Market = "us", Notional = 100_000m, CostPerSide = 0.0025 }),
            NullLogger<ShadowPortfolioJob>.Instance);

        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        await job.Execute(context);
    }

    private ShadowPortfolio LoadRetirement()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        return db.Set<ShadowPortfolio>()
            .Include(p => p.Positions)
            .Single(p => p.TemplateKey == RetirementKey);
    }

    [Fact]
    public async Task First_run_creates_and_invests_a_shadow_portfolio_per_template_paying_inception_cost()
    {
        await SetPricesAsync(100.0);

        await RunShadowJobAsync();

        ShadowPortfolio retirement = LoadRetirement();
        retirement.IsInvested.Should().BeTrue();
        retirement.Notional.Should().Be(100_000m);
        retirement.Positions.Should().OnlyContain(p => Etfs.Contains(p.Symbol));

        // Inception buys the whole book from cash: 100k × 25 bps = 250 cost,
        // so the first NAV is ~99,750 (the retirement template is all ETFs).
        retirement.LastNav.Should().BeApproximately(99_750, 5.0);
        retirement.LastRebalancedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task Job_is_idempotent_within_a_day()
    {
        await SetPricesAsync(100.0);
        await RunShadowJobAsync();
        await RunShadowJobAsync(); // second run same day must not double-snapshot

        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        Guid id = db.Set<ShadowPortfolio>().Single(p => p.TemplateKey == RetirementKey).Id;
        int snapshots = db.Set<ShadowSnapshot>().Count(s => s.ShadowPortfolioId == id);
        snapshots.Should().Be(1);
    }

    [Fact]
    public async Task The_public_endpoint_returns_the_series_labeled_costs_simulated()
    {
        await SetPricesAsync(100.0);
        await RunShadowJobAsync();

        // Anonymous — a trust asset, no auth.
        HttpResponseMessage response = await Client.GetAsync("/api/shadow-track-record");
        response.EnsureSuccessStatusCode();
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("disclaimer").GetString().Should().Contain("costs simulated");

        JsonElement portfolios = body.GetProperty("portfolios");
        JsonElement retirement = portfolios.EnumerateArray()
            .Single(p => p.GetProperty("templateKey").GetString() == RetirementKey);

        retirement.GetProperty("notional").GetDecimal().Should().Be(100_000m);
        retirement.GetProperty("series").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        // First snapshot reflects the inception cost drag.
        retirement.GetProperty("totalReturn").GetDouble().Should().BeLessThan(0);
    }
}
