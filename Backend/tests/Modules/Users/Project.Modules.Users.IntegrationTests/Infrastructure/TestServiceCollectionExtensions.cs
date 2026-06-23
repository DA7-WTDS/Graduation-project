using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Project.Modules.Users.IntegrationTests.Infrastructure;

internal static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Removes background <see cref="IHostedService"/> registrations whose implementation
    /// type belongs to one of the given namespaces. Used to switch off the Quartz
    /// outbox/inbox jobs and the MassTransit RabbitMQ bus so integration tests run
    /// deterministically without a broker.
    /// </summary>
    public static IServiceCollection RemoveHostedServices(
        this IServiceCollection services,
        params string[] implementationNamespaceContains)
    {
        List<ServiceDescriptor> toRemove = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .Where(d =>
            {
                string? fullName = d.ImplementationType?.FullName
                    ?? d.ImplementationInstance?.GetType().FullName;
                return fullName is not null
                    && implementationNamespaceContains.Any(n =>
                        fullName.Contains(n, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        foreach (ServiceDescriptor descriptor in toRemove)
        {
            services.Remove(descriptor);
        }

        return services;
    }
}
