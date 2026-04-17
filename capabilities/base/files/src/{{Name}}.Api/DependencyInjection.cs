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
        services.AddOpenApi();
        services.AddProblemDetails();
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
        // Route groups go here; the `/add-endpoint` skill appends to this method.
        return app;
    }
}
