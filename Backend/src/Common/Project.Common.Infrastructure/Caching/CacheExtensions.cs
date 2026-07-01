using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Common.Application.Caching;

namespace Project.Common.Infrastructure.Caching;

public static class CacheExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCachingInternal(string redisConnectionString, bool demoMode = false)
        {
            if (demoMode)
            {
                // No Redis server in demo hosting — HybridCache uses an in-process
                // distributed cache. Behaviour is identical for a single instance;
                // the cache is simply lost on restart.
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
            }

            services.ConfigureOptions<HybridCacheConfigureOptions>();

            services.AddHybridCache();

            services.TryAddScoped<ICacheService, CacheService>();

            return services;
        }
    }
}
