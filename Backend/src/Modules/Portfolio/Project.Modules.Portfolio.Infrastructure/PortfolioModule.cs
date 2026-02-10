using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        // Register DbContext
        services.AddDbContext<PortfolioDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Database")));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PortfolioDbContext>());

        // Register Repositories
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();

        // Register Public API
        services.AddScoped<IPortfolioApi, PortfolioApi>();

        return services;
    }
}
