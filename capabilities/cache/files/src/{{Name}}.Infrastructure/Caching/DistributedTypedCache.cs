using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace {{Name}}.Infrastructure.Caching;

internal sealed class DistributedTypedCache(IDistributedCache cache) : ITypedCache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? null : JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl,
        }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => cache.RemoveAsync(key, ct);

    public async Task<T> GetOrAddAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default) where T : class
    {
        var hit = await GetAsync<T>(key, ct);
        if (hit is not null) return hit;

        var value = await factory(ct);
        await SetAsync(key, value, ttl, ct);
        return value;
    }
}
