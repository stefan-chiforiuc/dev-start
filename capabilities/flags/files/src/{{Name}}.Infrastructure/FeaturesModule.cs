using Microsoft.Extensions.DependencyInjection;
using OpenFeature;
using OpenFeature.Providers.Memory;
using {{Name}}.Application.Features;
using {{Name}}.Infrastructure.Features;

namespace {{Name}}.Infrastructure;

internal static class FeaturesModule
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services)
    {
        // In-memory provider for dev/tests. Swap to your provider (LaunchDarkly,
        // Unleash, GrowthBook, ConfigCat) by registering a different IFeatureProvider.
        var provider = new InMemoryProvider(new Dictionary<string, Flag>
        {
            ["orders.new-flow-enabled"] = new Flag<bool>(
                variants: new Dictionary<string, bool> { ["on"] = true, ["off"] = false },
                defaultVariant: "off"),
        });

        OpenFeature.Api.Instance.SetProviderAsync(provider).GetAwaiter().GetResult();

        services.AddSingleton(sp => OpenFeature.Api.Instance.GetClient("{{namelower}}"));
        services.AddSingleton<IFeatureFlags, OpenFeatureFlags>();
        return services;
    }
}
