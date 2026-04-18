using {{Name}}.Api;
using {{Name}}.Application;
using {{Name}}.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

// devstart:program-before-services

builder.Services
    .AddApiServices(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

// devstart:program-after-services

var app = builder.Build();

// devstart:program-before-middleware

app.UseSerilogRequestLogging();
app.UseProblemDetailsErrorHandling();

app.UseSwagger();                        // serves /swagger/v1/swagger.json
app.UseSwaggerUI(ui => ui.SwaggerEndpoint("/swagger/v1/swagger.json", "API"));
// Scalar's docs UI is optional; enable once you've pinned the API surface:
//     app.MapScalarApiReference();

app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapHealthChecks("/readyz",
    new() { Predicate = h => h.Tags.Contains("ready", StringComparer.Ordinal) }).AllowAnonymous();

// devstart:program-endpoints
app.MapEndpoints();

await app.RunAsync();

public partial class Program;
