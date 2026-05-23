using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{Name}}.Infrastructure.Caching;

namespace {{Name}}.Infrastructure;

internal static class CacheModule
{
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();
        services.AddSingleton<ITypedCache, MemoryTypedCache>();
        return services;
    }
}
