using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{Name}}.Api.Auth;

internal static class AuthModule
{
    public static IServiceCollection AddDevStartAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Auth:Authority"]
            ?? throw new InvalidOperationException("Missing Auth:Authority");
        var audience = config["Auth:Audience"]
            ?? throw new InvalidOperationException("Missing Auth:Audience");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false; // dev-only; override per environment
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}
