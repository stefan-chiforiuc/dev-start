namespace {{Name}}.Application.Features;

/// <summary>
/// Thin wrapper over OpenFeature so callers don't depend on the provider.
/// Short-lived flags only — every flag has an owner and a removal date.
/// </summary>
public interface IFeatureFlags
{
    Task<bool> IsEnabledAsync(string flag, string? userId = null, CancellationToken ct = default);
}
