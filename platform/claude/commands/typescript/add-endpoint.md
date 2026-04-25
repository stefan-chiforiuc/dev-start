---
description: Scaffold a new Fastify endpoint group following the Orders template
argument-hint: <resource> (plural, kebab-case)
---

Create a new endpoint group for `$ARGUMENTS` following the exact shape of
the Orders sample:

1. **Schemas** — `apps/api/src/$ARGUMENTS/schemas.ts`:
   - Zod schemas for every request body, response body, and path param.
   - Export inferred TS types for handlers to import.
2. **Repo** — `apps/api/src/$ARGUMENTS/repo.ts`:
   - One exported function per use case. Takes `Kysely<Database>`. Never
     exposes the raw query builder.
3. **Routes** — `apps/api/src/$ARGUMENTS/routes.ts`:
   - `FastifyPluginAsync` exporting the plugin.
   - Handlers parse with zod (422 on failure), delegate to repo, map
     errors to RFC-7807 via `toProblemDetails`.
4. **Wire it** — in `apps/api/src/app.ts`:
   - Add `import { $ARGUMENTSRoutes } from "./$ARGUMENTS/routes.js";` after
     the `@fastify/cors` import.
   - Inside `buildApp`, register at `// devstart:app-routes`:
     `await app.register($ARGUMENTSRoutes, { prefix: "/$ARGUMENTS" });`
5. **Migration** — invoke `/add-migration create-$ARGUMENTS` and review
   before applying.
6. **Test** — `apps/api/test/$ARGUMENTS.test.ts`. Use `getApp()` from
   `test/helpers.ts`; inject HTTP with `app.inject({ method, url, payload })`.

Stick to the house style; deviating from the Orders shape needs an ADR.
