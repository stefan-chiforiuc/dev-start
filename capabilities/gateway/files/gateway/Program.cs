using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = builder.Configuration["Auth:Authority"];
        o.Audience = builder.Configuration["Auth:Audience"];
        o.RequireHttpsMetadata = false;
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok()).AllowAnonymous();

app.MapReverseProxy(pipeline =>
{
    // Forward user claims as X-User-* headers so downstream services can read them.
    pipeline.Use(async (ctx, next) =>
    {
        var user = ctx.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            ctx.Request.Headers["X-User-Id"] = user.FindFirst("sub")?.Value ?? "";
            ctx.Request.Headers["X-User-Name"] = user.Identity?.Name ?? "";
        }
        await next();
    });
});

await app.RunAsync();
