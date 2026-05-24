using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{Name}}.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHealthChecks();
        services.AddEndpointsApiExplorer();
        // Swashbuckle generates the OpenAPI document; Scalar renders a docs UI
        // from it. Kept on Swashbuckle (not .NET 9's built-in AddOpenApi) so the
        // target framework stays on the LTS .NET 8.
        services.AddSwaggerGen();
        services.AddProblemDetails();
        // devstart:api-services
        return services;
    }

    public static WebApplication UseProblemDetailsErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // devstart:api-endpoints
        return app;
    }
}
