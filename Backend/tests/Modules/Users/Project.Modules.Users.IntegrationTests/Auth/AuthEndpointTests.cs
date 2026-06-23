using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Project.Modules.Users.IntegrationTests.Infrastructure;

namespace Project.Modules.Users.IntegrationTests.Auth;

// Covers FR-01 (registration + BCrypt + duplicate rejection) and FR-02 (JWT login).
public sealed class AuthEndpointTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_then_login_returns_a_jwt()
    {
        HttpResponseMessage register = await Client.PostAsJsonAsync("/api/users/register",
            new { email = "alice@quantwise.test", password = DefaultPassword, firstName = "Alice", lastName = "Doe" });

        register.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage login = await Client.PostAsJsonAsync("/api/users/login",
            new { email = "alice@quantwise.test", password = DefaultPassword });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await login.Content.ReadFromJsonAsync<JsonElement>();
        string token = body.GetProperty("accessToken").GetString()!;
        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3, "a JWT has header.payload.signature");
    }

    [Fact]
    public async Task Register_with_a_duplicate_email_is_rejected()
    {
        var payload = new { email = "dup@quantwise.test", password = DefaultPassword, firstName = "Dup", lastName = "User" };

        HttpResponseMessage first = await Client.PostAsJsonAsync("/api/users/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage second = await Client.PostAsJsonAsync("/api/users/register", payload);
        second.IsSuccessStatusCode.Should().BeFalse();
        ((int)second.StatusCode).Should().BeOneOf(400, 409);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_unauthorized()
    {
        await Client.PostAsJsonAsync("/api/users/register",
            new { email = "bob@quantwise.test", password = DefaultPassword, firstName = "Bob", lastName = "Doe" });

        HttpResponseMessage login = await Client.PostAsJsonAsync("/api/users/login",
            new { email = "bob@quantwise.test", password = "wrong-password" });

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
