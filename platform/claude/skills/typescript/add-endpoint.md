---
name: add-endpoint
description: Scaffold a new Fastify endpoint as a full vertical slice.
---

# /add-endpoint

Produce a full vertical slice for a new endpoint:

1. Create `apps/api/src/<domain>/<verb>.ts` with a typed handler.
2. Create `apps/api/src/<domain>/<verb>.schema.ts` — the zod input/output
   schemas; export the inferred TS types.
3. Register the route in `apps/api/src/<domain>/routes.ts` (or create it).
4. Register the plugin in `apps/api/src/app.ts` at the
   `// devstart:app-plugins` marker, and mount its prefix at
   `// devstart:app-routes`.
5. Add a Vitest test in `apps/api/test/<domain>.test.ts` that boots the
   Fastify app via the helper in `apps/api/test/helpers.ts` and hits the
   new route.

Keep handlers thin: parse with zod, delegate to a service function, map
errors to RFC-7807 problem+json via the shared `toProblemDetails()` util.
