# `cache-memory`

In-process `IMemoryCache`-backed implementation of `ITypedCache`. Same
public surface as the `cache` (Redis) variant, no docker dependency.

## Wires

- `src/My.Infrastructure/Caching/ITypedCache.cs` — the shared interface
  (also installed by `cache`, identical content).
- `src/My.Infrastructure/Caching/MemoryTypedCache.cs` — `IMemoryCache`
  implementation.
- `src/My.Infrastructure/CacheModule.cs` — `services.AddMemoryCache()`
  + `ITypedCache → MemoryTypedCache`.
- Injects `services.AddCache(config);` into `DependencyInjection.cs`.

## Why pick this over `cache`?

- Single-instance apps that don't need cross-process invalidation.
- Tests that want a cache without a docker dependency.
- Greenfield projects that start small and swap to Redis later via
  `dev-start add cache --engine redis` (after removing `cache-memory`).

## Escape hatches

- Add a TTL sweep / size cap by configuring `MemoryCacheOptions` in
  `Program.cs`.
- For distributed scenarios, swap to the `cache` capability — same
  `ITypedCache` consumers keep working.
