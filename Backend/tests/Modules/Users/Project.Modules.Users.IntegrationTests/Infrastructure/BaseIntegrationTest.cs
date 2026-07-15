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

    /// <summary>Raw questionnaire answers — the only way to onboard now that the
    /// legacy portfolio row is gone (§ 4.7). Defaults score to an Aggressive
    /// long-term-wealth profile; override to shift the band.</summary>
    protected static object SampleQuestionnaireBody(
        string goalType = "long_term_wealth",
        string engagement = "monthly",
        string marketReaction = "buy_more",
        string experience = "experienced",
        decimal investment = 10000m) => new
        {
            goalId = (Guid?)null,
            goalType,
            horizonYears = 10,
            investmentAmount = investment,
            monthlyContribution = 0m,
            hasEmergencyFund = true,
            incomeStability = "stable",
            savingsShare = "less_than_ten_percent",
            marketReaction,
            experience,
            engagement,
            usdComfort = "comfortable",
            affordLossConfirmed = false,
        };

    /// <summary>Completes onboarding for the currently authorized user.</summary>
    protected async Task<Guid> OnboardAsync(object? body = null)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/goals/questionnaire", body ?? SampleQuestionnaireBody());
        response.EnsureSuccessStatusCode();
        JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("goalId").GetGuid();
    }
}
