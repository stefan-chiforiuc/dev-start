using {{Name}}.Api;
using {{Name}}.Application;
using {{Name}}.Infrastructure;
using Scalar.AspNetCore;
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

app.MapOpenApi();
app.MapScalarApiReference("/docs");

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz", new() { Predicate = h => h.Tags.Contains("ready") });

// devstart:program-endpoints
app.MapEndpoints();

await app.RunAsync();

public partial class Program;
