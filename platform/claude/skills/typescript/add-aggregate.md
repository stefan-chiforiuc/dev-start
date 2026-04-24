---
name: add-aggregate
description: Add a new domain module (aggregate + zod schema + repo).
---

# /add-aggregate

1. Create `apps/api/src/<name>/<name>.ts` exporting the domain type and
   constructor functions (no classes — prefer pure functions).
2. Create `apps/api/src/<name>/<name>.schema.ts` with the zod schema for
   validating inputs at the HTTP/event boundary.
3. Create `apps/api/src/<name>/<name>.repo.ts` with a Kysely-backed repo:
   one function per use case (don't expose the raw query builder).
4. Create a migration (`/add-migration`) for the new table.
5. Register a plugin (`routes.ts`) at `// devstart:app-plugins`.
6. Unit-test the pure functions; integration-test the repo + routes.
