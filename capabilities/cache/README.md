# cache capability

Redis-backed distributed cache with a typed wrapper.

## Wires

- Redis in compose.
- `StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis`.
- `ITypedCache<T>` — JSON-serialised get/set/remove with a sensible default TTL.
- Cache-aside extension: `cache.GetOrAddAsync("key", TimeSpan, factory)`.
- Health check against Redis.

## Opinions

- **Cache-aside over write-through.** Simpler to reason about.
- **Opaque keys.** Key builders are per-module, not a central "key factory".
- **Default TTL = 5 min.** Longer needs an ADR.

## Escape hatches

- In-memory cache for single-instance apps: set `Redis__Connection` empty;
  the wrapper falls back to `IMemoryCache`.
