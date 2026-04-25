# ts-base

The substrate for the TypeScript/Fastify stack. Ships a pnpm workspace with
one `apps/api/` Fastify app scaffold. Other `ts-*` capabilities extend this
via file copies and injectors into `apps/api/src/app.ts`.

## What you get

- `package.json` + `pnpm-workspace.yaml`
- `apps/api/` — Fastify 5, Node 22, strict TypeScript, ESM
- `apps/api/src/app.ts` with the `// devstart:app-plugins` and
  `// devstart:app-routes` injection markers
- Vitest config and a smoke test
- `Dockerfile` (multi-stage, distroless)
- `justfile` with `bootstrap / up / test / build`
