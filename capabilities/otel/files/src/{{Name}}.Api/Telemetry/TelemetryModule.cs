using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace {{Name}}.Api.Telemetry;

internal static class TelemetryModule
{
    public static IServiceCollection AddDevStartTelemetry(this IServiceCollection services, IConfiguration config)
    {
        var serviceName = config["OTEL_SERVICE_NAME"] ?? "{{name}}";
        var endpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"];

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName: serviceName)
                .AddAttributes(new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["deployment.environment"] = config["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                }))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                t.AddHttpClientInstrumentation();
                t.AddSource("{{Name}}.*");
                if (!string.IsNullOrEmpty(endpoint))
                {
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
                }
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation();
                m.AddHttpClientInstrumentation();
                m.AddRuntimeInstrumentation();
                if (!string.IsNullOrEmpty(endpoint))
                {
                    m.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
                }
            });

        return services;
    }
}
