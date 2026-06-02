using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Project.Common.Infrastructure;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.Configuration;
using Project.Modules.Recommendations.Infrastructure.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Recommendations.Infrastructure.Llm;

namespace Project.Modules.Recommendations.Infrastructure;

public static class RecommendationsModule
{
    public static IServiceCollection AddRecommendationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddModuleEndpoints(Presentation.AssemblyReference.Assembly);

        services.AddDbContextPool<RecommendationsDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    sp.GetRequiredService<NpgsqlDataSource>(),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Recommendations))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RecommendationsDbContext>());
        services.AddScoped<IDailyRunRepository, DailyRunRepository>();

        services.Configure<LlmOptions>(configuration.GetSection("Recommendations:Llm"));
        services.Configure<IngestOptions>(configuration.GetSection("Recommendations:Ingest"));

        services.AddHttpClient<ILlmClient, GeminiLlmClient>((sp, client) =>
        {
            LlmOptions o = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
            client.BaseAddress = new Uri(o.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(o.ApiKey))
            {
                // Native Gemini endpoint authenticates via the x-goog-api-key header.
                client.DefaultRequestHeaders.Add("x-goog-api-key", o.ApiKey);
            }
        });

        return services;
    }
}
