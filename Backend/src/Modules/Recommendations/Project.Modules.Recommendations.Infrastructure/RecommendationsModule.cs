using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Project.Common.Infrastructure;
using Project.Common.Application.Messaging;
using Project.Common.Infrastructure.Outbox;
using Project.Common.Infrastructure.Inbox;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Application.Abstractions.Holdings;
using Project.Modules.Recommendations.Application.Abstractions.Llm;
using Project.Modules.Recommendations.Application.Configuration;
using Project.Modules.Recommendations.Infrastructure.DailyRuns;
using Project.Modules.Recommendations.Infrastructure.Database;
using Project.Modules.Recommendations.Infrastructure.Holdings;
using Project.Modules.Recommendations.Infrastructure.Llm;
using Project.Modules.Recommendations.Infrastructure.Outbox;
using Project.Modules.Recommendations.Infrastructure.Inbox;
using Project.Modules.Recommendations.Infrastructure.Pipeline;
using Project.Modules.Recommendations.Infrastructure.PublicApi;
using Project.Modules.Recommendations.PublicApi;
using MassTransit;

namespace Project.Modules.Recommendations.Infrastructure;

public static class RecommendationsModule
{
    public static IServiceCollection AddRecommendationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDomainEventHandlers(typeof(IdempotentDomainEventHandler<>), Application.AssemblyReference.Assembly)
            .AddIntegrationEventHandlers(typeof(IdempotentIntegrationEventHandler<>), Presentation.AssemblyReference.Assembly)
            .AddModuleEndpoints(Presentation.AssemblyReference.Assembly);

        services.AddInfrastructure(configuration);

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with Outbox Interceptor
        services.AddDbContextPool<RecommendationsDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    sp.GetRequiredService<NpgsqlDataSource>(),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Recommendations))
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RecommendationsDbContext>());
        services.AddScoped<IDailyRunRepository, DailyRunRepository>();
        services.AddScoped<IUserHoldingRepository, UserHoldingRepository>();

        // Register Public API
        services.AddScoped<IRecommendationsApi, RecommendationsApi>();

        // Llm and Ingest configurations
        services.Configure<LlmOptions>(configuration.GetSection("Recommendations:Llm"));
        services.Configure<IngestOptions>(configuration.GetSection("Recommendations:Ingest"));

        // Outbox & Inbox configurations
        services.Configure<OutboxOptions>(configuration.GetSection("Recommendations:Outbox"));
        services.Configure<InboxOptions>(configuration.GetSection("Recommendations:Inbox"));

        // Register Quartz Jobs
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        services.ConfigureOptions<ConfigureProcessInboxJob>();
        services.ConfigureOptions<ConfigureFetchDailyPipelineJob>();

        // Pipeline HTTP client — typed client for FetchDailyPipelineJob
        services.Configure<PipelineOptions>(configuration.GetSection("Recommendations:Pipeline"));
        services.AddHttpClient<FetchDailyPipelineJob>((sp, client) =>
        {
            PipelineOptions o = sp.GetRequiredService<IOptions<PipelineOptions>>().Value;
            client.BaseAddress = new Uri(o.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
        });

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

    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        // Add consumers for any integration events consumed by this module in the future
    }
}

