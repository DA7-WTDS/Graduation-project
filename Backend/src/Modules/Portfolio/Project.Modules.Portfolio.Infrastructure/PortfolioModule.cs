using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Project.Common.Infrastructure;
using Project.Modules.Portfolio.Application.Abstractions.Data;
using Project.Modules.Portfolio.Application.Abstractions.Portfolios;
using Project.Modules.Portfolio.Infrastructure.Database;
using Project.Modules.Portfolio.Infrastructure.Portfolios;
using Project.Modules.Portfolio.Infrastructure.PublicApi;
using Project.Modules.Portfolio.PublicApi;

namespace Project.Modules.Portfolio.Infrastructure;

public static class PortfolioModule
{
    public static IServiceCollection AddPortfolioModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register endpoints from the Presentation layer
        services.AddModuleEndpoints(Presentation.AssemblyReference.Assembly);

        // Register DbContext
        services.AddDbContextPool<PortfolioDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    sp.GetRequiredService<NpgsqlDataSource>(),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Portfolio))
                .UseSnakeCaseNamingConvention());

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PortfolioDbContext>());

        // Register Repositories
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();

        // Register Public API
        services.AddScoped<IPortfolioApi, PortfolioApi>();

        return services;
    }
}

