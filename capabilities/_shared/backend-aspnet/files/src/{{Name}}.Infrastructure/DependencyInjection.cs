using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{Name}}.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        // devstart:infrastructure-services
        return services;
    }
}
