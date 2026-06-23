using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Project.Modules.Users.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests: gives each test a fresh database (Respawn reset),
/// an <see cref="HttpClient"/> against the in-memory server, and helpers for the common
/// register/login dance.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    protected const string DefaultPassword = "P@ssw0rd123!";

    protected IntegrationTestWebAppFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<(Guid UserId, string Token)> RegisterAndLoginAsync(string email)
    {
        HttpResponseMessage register = await Client.PostAsJsonAsync("/api/users/register",
            new { email, password = DefaultPassword, firstName = "Test", lastName = "User" });
        register.EnsureSuccessStatusCode();
        JsonElement created = await register.Content.ReadFromJsonAsync<JsonElement>();
        Guid userId = created.GetProperty("id").GetGuid();

        HttpResponseMessage login = await Client.PostAsJsonAsync("/api/users/login",
            new { email, password = DefaultPassword });
        login.EnsureSuccessStatusCode();
        JsonElement body = await login.Content.ReadFromJsonAsync<JsonElement>();
        string token = body.GetProperty("accessToken").GetString()!;

        return (userId, token);
    }

    protected void Authorize(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    protected static object SamplePortfolioBody(int stocks = 60, string riskProfile = "Moderate", decimal investment = 10000m) => new
    {
        primaryGoal = "wealth",
        timeHorizon = "long",
        riskTolerance = 5,
        marketReaction = "hold",
        investmentExperience = "intermediate",
        stocksPercentage = stocks,
        bondsPercentage = 20,
        etfsPercentage = 100 - stocks - 20 - 5,
        cashPercentage = 5,
        riskProfile,
        investmentAmount = investment,
    };
}
