using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Project.Modules.Notifications.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.Abstractions.Pipeline;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Users.Infrastructure.Database;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Project.Modules.Users.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API in-memory (TestServer) against throwaway Postgres and Redis
/// containers. External, non-deterministic dependencies are swapped for test doubles
/// (the LLM client) and the background workers (Quartz, MassTransit) are switched off,
/// so each flow is exercised end-to-end through HTTP, MediatR, EF Core, and a real
/// PostgreSQL database without reaching any external service.
/// </summary>
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string IngestApiKey = "integration-test-pipeline-key";
    private const string TestSigningKey = "integration-test-signing-key-please-keep-this-32+bytes-long";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithDatabase("quantwise_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:8-alpine")
        .Build();

    private NpgsqlConnection _respawnConnection = null!;
    private Respawner _respawner = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override the secrets/connection strings the app reads. Added after the app's own
        // sources, so these win over anything DotNetEnv loaded from the repo .env.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["Authentication:Key"] = TestSigningKey,
                ["Authentication:Authority"] = "https://quantwise.test",
                ["Authentication:Audience"] = "quantwise-test",
                ["Authentication:ExpiresInMinutes"] = "60",
                ["Recommendations:Ingest:ApiKey"] = IngestApiKey,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Program reads the DB/Redis connection strings before the factory's config
            // is merged, so point them at the containers at the service level instead.
            services.RemoveAll<NpgsqlDataSource>();
            services.AddNpgsqlDataSource(_postgres.GetConnectionString());
            services.Configure<RedisCacheOptions>(o => o.Configuration = _redis.GetConnectionString());

            // Never call the real Gemini API.
            services.RemoveAll<ILlmClient>();
            services.AddSingleton<ILlmClient, FakeLlmClient>();

            // Never call the real pipeline for § 6.3 prediction audits.
            services.RemoveAll<IPipelineReproducer>();
            services.AddSingleton<IPipelineReproducer, FakePipelineReproducer>();

            // Determinism: no broker, no background jobs writing to the DB mid-test.
            services.RemoveHostedServices("Quartz", "MassTransit");
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Touching Services builds the host (using the container connection strings above)
        // and lets us apply every module's migrations against the fresh database.
        using (IServiceScope scope = Services.CreateScope())
        {
            IServiceProvider sp = scope.ServiceProvider;
            await sp.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<PortfolioDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<RecommendationsDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        }

        _respawnConnection = new NpgsqlConnection(_postgres.GetConnectionString());
        await _respawnConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["users", "Portfolio", "Recommendations", "notifications"],
            TablesToIgnore =
            [
                new Table("users", "__EFMigrationsHistory"),
                new Table("Portfolio", "__EFMigrationsHistory"),
                new Table("Recommendations", "__EFMigrationsHistory"),
                new Table("notifications", "__EFMigrationsHistory"),
                // Reference data seeded by migration (§ 3.1 / § 3.2) — not per-test state.
                new Table("Portfolio", "instruments"),
                new Table("Portfolio", "strategy_templates"),
            ],
        });
    }

    /// <summary>Truncates all module data between tests, leaving the schema intact.</summary>
    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_respawnConnection);

    public new async Task DisposeAsync()
    {
        await _respawnConnection.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
