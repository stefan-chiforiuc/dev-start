using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {{Name}}.Application.Email;
using {{Name}}.Infrastructure.Email;

namespace {{Name}}.Infrastructure;

internal static class EmailModule
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        return services;
    }
}
