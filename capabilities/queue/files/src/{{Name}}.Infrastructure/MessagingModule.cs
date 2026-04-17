using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{Name}}.Application.Messaging;
using {{Name}}.Infrastructure.Messaging;
using {{Name}}.Infrastructure.Persistence;

namespace {{Name}}.Infrastructure;

internal static class MessagingModule
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(mt =>
        {
            mt.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            mt.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(
                    config["RabbitMq:Host"] ?? "localhost",
                    h =>
                    {
                        h.Username(config["RabbitMq:Username"] ?? "guest");
                        h.Password(config["RabbitMq:Password"] ?? "guest");
                    });
                cfg.ConfigureEndpoints(ctx);
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromMinutes(1),
                    intervalDelta: TimeSpan.FromSeconds(5)));
            });
        });

        return services;
    }
}
