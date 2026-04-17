using OpenFeature;
using OpenFeature.Model;
using {{Name}}.Application.Features;

namespace {{Name}}.Infrastructure.Features;

internal sealed class OpenFeatureFlags(FeatureClient client) : IFeatureFlags
{
    public async Task<bool> IsEnabledAsync(string flag, string? userId = null, CancellationToken ct = default)
    {
        var ctx = EvaluationContext.Builder()
            .Set("targetingKey", userId ?? "anonymous")
            .Build();
        return await client.GetBooleanValueAsync(flag, defaultValue: false, context: ctx);
    }
}
