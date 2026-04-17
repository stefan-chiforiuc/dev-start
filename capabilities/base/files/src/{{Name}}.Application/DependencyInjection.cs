using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace {{Name}}.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var asm = Assembly.GetExecutingAssembly();
        services.AddMediatR(c => c.RegisterServicesFromAssembly(asm));
        services.AddValidatorsFromAssembly(asm);
        // devstart:application-services
        return services;
    }
}
