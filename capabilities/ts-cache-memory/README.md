# `ts-cache-memory`

In-process `Map`-backed cache with per-entry TTL. Same `app.cache` API
as the Redis-backed `ts-cache`, no `REDIS_URL` required, no docker
service.

## Wires

- `apps/api/src/infra/cache.ts` — the plugin (`cachePlugin`).
- Registers the plugin in `apps/api/src/app.ts` at the
  `// devstart:app-plugins` marker.

## Why pick this over `ts-cache`?

- Single-instance services that don't need cross-process invalidation.
- Tests and CI that want to skip the Redis container.
- Greenfield apps that start simple and swap to Redis later:
  `dev-start add cache --engine redis` (after removing
  `ts-cache-memory`).
