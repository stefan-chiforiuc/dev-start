using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{Name}}.Infrastructure.Persistence;
using {{Name}}.Infrastructure.Persistence.Interceptors;

namespace {{Name}}.Infrastructure;

internal static class PostgresModule
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<DomainEventsInterceptor>(sp =>
            new DomainEventsInterceptor(sp.GetRequiredService<IPublisher>()));

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var cs = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");
            options
                .UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations", "app"))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<DomainEventsInterceptor>());
        });

        services.AddHealthChecks()
            .AddNpgSql(
                connectionStringFactory: sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!,
                name: "postgres",
                tags: ["ready"]);

        return services;
    }
}
