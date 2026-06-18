using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Portfolios;

// Covers FR-05/06 (onboarding -> portfolio), FR-07 (retake updates in place),
// FR-08 (read own portfolio), and FR-04/NFR-04 (owner-only access, no IDOR).
public sealed class PortfolioEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Create_then_get_mine_returns_the_portfolio()
    {
        (_, string token) = await RegisterAndLoginAsync("owner@quantwise.test");
        Authorize(token);

        HttpResponseMessage create = await Client.PostAsJsonAsync("/api/portfolios", SamplePortfolioBody());
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage mine = await Client.GetAsync("/api/portfolios/me");
        mine.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await mine.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("stocksPercentage").GetInt32().Should().Be(60);
    }

    [Fact]
    public async Task Retake_updates_the_existing_portfolio_without_duplicating()
    {
        (Guid userId, string token) = await RegisterAndLoginAsync("retake@quantwise.test");
        Authorize(token);

        HttpResponseMessage create = await Client.PostAsJsonAsync("/api/portfolios", SamplePortfolioBody(stocks: 60));
        JsonElement created = await create.Content.ReadFromJsonAsync<JsonElement>();
        Guid portfolioId = created.GetProperty("id").GetGuid();

        HttpResponseMessage update = await Client.PutAsJsonAsync(
            $"/api/portfolios/{portfolioId}", SamplePortfolioBody(stocks: 70, riskProfile: "Aggressive"));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Exactly one portfolio row for the user, with the updated allocation.
        using IServiceScope scope = Factory.Services.CreateScope();
        PortfolioDbContext db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        List<Project.Modules.Portfolio.Domain.Portfolios.Portfolio> rows =
            db.Set<Project.Modules.Portfolio.Domain.Portfolios.Portfolio>()
              .Where(p => p.UserId == userId).ToList();

        rows.Should().ContainSingle();
        rows[0].StocksPercentage.Should().Be(70);
    }

    [Fact]
    public async Task A_user_cannot_read_another_users_portfolio_by_id()
    {
        // User A creates a portfolio.
        (_, string tokenA) = await RegisterAndLoginAsync("a@quantwise.test");
        Authorize(tokenA);
        HttpResponseMessage create = await Client.PostAsJsonAsync("/api/portfolios", SamplePortfolioBody());
        Guid portfolioId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // User B tries to read it by id.
        (_, string tokenB) = await RegisterAndLoginAsync("b@quantwise.test");
        Authorize(tokenB);
        HttpResponseMessage forbidden = await Client.GetAsync($"/api/portfolios/{portfolioId}");

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Creating_a_portfolio_without_a_token_is_unauthorized()
    {
        HttpResponseMessage create = await Client.PostAsJsonAsync("/api/portfolios", SamplePortfolioBody());
        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
