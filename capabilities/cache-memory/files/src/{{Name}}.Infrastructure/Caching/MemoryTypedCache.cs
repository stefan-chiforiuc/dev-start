using Microsoft.Extensions.Caching.Memory;

namespace {{Name}}.Infrastructure.Caching;

internal sealed class MemoryTypedCache(IMemoryCache cache) : ITypedCache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        cache.Set(key, value, ttl ?? DefaultTtl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

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
