using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{Name}}.Infrastructure.Caching;

namespace {{Name}}.Infrastructure;

internal static class CacheModule
{
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration config)
    {
        var connection = config["Redis:Connection"];

        if (string.IsNullOrWhiteSpace(connection))
        {
            // Fallback for single-instance apps or tests: in-memory cache.
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connection;
                options.InstanceName = "{{name}}:";
            });
        }

        services.AddSingleton<ITypedCache, DistributedTypedCache>();
        return services;
    }
}
