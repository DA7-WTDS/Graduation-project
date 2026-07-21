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
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Goals;
using Project.Modules.Portfolio.Application.Abstractions.Instruments;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Application.Abstractions.Proposals;
using Project.Modules.Portfolio.Application.Abstractions.Strategies;
using Project.Modules.Portfolio.Application.Abstractions.Shadow;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Goals;
using Project.Modules.Portfolio.Infrastructure.Instruments;
using Project.Modules.Portfolio.Infrastructure.Portfolios;
using Project.Modules.Portfolio.Infrastructure.Proposals;
using Project.Modules.Portfolio.Infrastructure.Strategies;
using Project.Modules.Portfolio.Infrastructure.Shadow;
using Project.Modules.Portfolio.Infrastructure.PublicApi;
using Project.Modules.Portfolio.Infrastructure.Outbox;
using Project.Modules.Portfolio.Infrastructure.Inbox;
using Project.Modules.Portfolio.PublicApi;
using MassTransit;

namespace Project.Modules.Portfolio.Infrastructure;

public static class PortfolioModule
{
    public static IServiceCollection AddPortfolioModule(
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
        services.AddDbContextPool<PortfolioDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    sp.GetRequiredService<NpgsqlDataSource>(),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Portfolio))
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())
                .UseSnakeCaseNamingConvention());

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PortfolioDbContext>());

        // Register Repositories
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<IStrategyTemplateRepository, StrategyTemplateRepository>();

        services.AddScoped<IPortfolioProposalRepository, PortfolioProposalRepository>();
        services.AddScoped<IGoalPortfolioRepository, GoalPortfolioRepository>();
        services.AddScoped<IShadowPortfolioRepository, ShadowPortfolioRepository>();

        // Shared goal→optimizer pipeline (draft preview + proposal creation).
        services.AddScoped<Application.Allocation.PortfolioProposalBuilder>();

        // Register Public API
        services.AddScoped<IPortfolioApi, PortfolioApi>();

        // Configure Outbox & Inbox options
        services.Configure<OutboxOptions>(configuration.GetSection("Portfolio:Outbox"));
        services.Configure<InboxOptions>(configuration.GetSection("Portfolio:Inbox"));

        // Register Quartz Jobs
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        services.ConfigureOptions<ConfigureProcessInboxJob>();
        services.ConfigureOptions<ConfigureRefreshInstrumentStatsJob>();
        services.ConfigureOptions<ConfigurePortfolioValuationJob>();
        services.Configure<PortfolioValuationOptions>(configuration.GetSection("Portfolio:Valuation"));
        services.ConfigureOptions<ConfigurePortfolioDigestJob>();
        services.Configure<PortfolioDigestOptions>(configuration.GetSection("Portfolio:Digest"));
        services.ConfigureOptions<ConfigureShadowPortfolioJob>();
        services.Configure<ShadowPortfolioOptions>(configuration.GetSection("Portfolio:Shadow"));
        services.Configure<Application.Shadow.GetShadowTrackRecord.ShadowTrackRecordOptions>(
            configuration.GetSection("Portfolio:Shadow"));

        // Instrument registry refresh — typed HTTP client against the pipeline
        services.Configure<InstrumentsOptions>(configuration.GetSection("Portfolio:Instruments"));
        services.AddHttpClient<RefreshInstrumentStatsJob>((sp, client) =>
        {
            InstrumentsOptions o = sp.GetRequiredService<IOptions<InstrumentsOptions>>().Value;
            client.BaseAddress = new Uri(o.PipelineBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
        });

        return services;
    }

    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        // Add consumers for any integration events consumed by this module in the future
    }
}


