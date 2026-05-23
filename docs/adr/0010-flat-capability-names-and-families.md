# 0010 — Flat capability names + family/variant model

- Status: Accepted
- Date: 2026-05-23
- Relates to: [ADR 0006](0006-capabilities-not-templates.md),
  [ADR 0007](0007-injectors-over-fork-templates.md),
  [ADR 0008](0008-ts-prefix-for-typescript-capabilities.md)

## Context

ADR 0008 established the `ts-` prefix on capability folder names — `ts-auth`,
`ts-postgres`, `ts-base` live as siblings of `auth`, `postgres`, `base`. That
decision was made for *contributor* discoverability inside the
`capabilities/` tree: one folder = one slice, no inner branching, easy
side-by-side comparison.

Three things have changed since:

1. **User-facing naming feedback.** The project's `.devstart.json` already
   records the stack. Forcing users to type `dev-start add ts-auth` instead
   of `dev-start add auth` leaks an internal detail. The same goes for
   `s3` not reading as ".NET-only" when typed in a TS project — users
   discover this only by error.
2. **The `deploy-fly` vs `deploy-aca` asymmetry.** They're mutually
   exclusive *by intent* but separate by *name*. Picking a deploy target
   should be a parameter on a single `deploy` capability, not a separate
   capability per target. The naming asymmetry leaks into wizard prompts,
   list output, and dependent capabilities.
3. **Framework + version selection.** The wizard work (this round) needs
   to ask "which ASP.NET version?" or "Fastify vs Nest". The current
   "one capability per opinion" model has no concept of grouped variants.

We need to address all three without throwing away ADR 0008's insight
(one folder per slice keeps internals simple).

## Decision

Keep the on-disk folder layout flat and prefixed — **ADR 0008 still
governs the filesystem**. Move the abstraction to a new
**capability resolver** layer that sits between user-typed names and
concrete folders.

### Three resolver rules

1. **Stack-prefixed match for TS projects.** `dev-start add auth` in a
   TS project resolves to `ts-auth`. Typing `ts-auth` explicitly remains
   an escape hatch.
2. **Exact match.** `dev-start add deploy-fly` still works for power
   users and for the upgrader.
3. **Family resolution.** When a name maps to a *family* (e.g. `deploy`,
   `backend`), the resolver picks the variant matching the user's
   parameters: `dev-start add deploy --target fly` → `deploy-fly` (or
   `ts-deploy-fly` per stack).

### New capability metadata (additive)

Four optional fields on `capability.json`. Existing capabilities omit
them; semantics are unchanged.

```jsonc
{
  "family": "backend",         // groups variants under one user-facing name
  "framework": "aspnet",       // wizard/resolver filter
  "frameworkVersion": "8",     // wizard prompt option
  "provides": ["base"]         // alias targets — dependents that say
                               // `dependsOn: ["base"]` resolve through here
}
```

`provides` matters for the family model: today `auth`'s
`dependsOn: ["base"]` resolves directly. Tomorrow when `base-aspnet-9`
and `base-aspnet-10` exist as separate folders, both will declare
`provides: ["base"]` and the resolver will pick whichever variant the
project chose. No churn in dependent capability JSONs.

### Manifest schema v3

Adds a `backend` block recording the framework + version the project was
scaffolded against:

```jsonc
{
  "schemaVersion": 3,
  "stack": "dotnet-api",
  "backend": { "framework": "aspnet", "version": "8" },
  ...
}
```

v2 → v3 migration infers the backend from `stack` + installed
capabilities (pre-v3 projects had exactly one backend variant per stack).

### `dev-start new` wizard

Runs by default when stdin is a TTY. Skipped when `--no-interactive` is
passed or when stdin is not a TTY (CI). Flags act as pre-answers —
the wizard skips any prompt whose value was provided by a flag.

## Consequences

**Positive**

- Users type stack-neutral names. `dev-start add auth` works in any
  stack. The CLI prints the resolved name (`resolved auth → ts-auth`)
  so behavior is never silent.
- `deploy --target fly|aca` reads as "one decision with two options",
  matching how teams actually think about it.
- The wizard surfaces all up-front decisions in one consistent
  experience. Defaults preserve scripted behavior (`--no-interactive`
  matches today's flag-driven flow).
- The family/variant model is the natural extension point for backend
  versions, frontend framework choice, and engine swaps (Postgres/MySQL,
  Redis/in-memory) without breaking ADR 0006's "one opinion per
  capability" rule — each variant is still its own folder.

**Negative**

- The resolver is a new layer of indirection. Mitigation: it's small
  (`CapabilityResolver.cs` ~190 LOC), documented, and the rules are
  printed at resolve time so users see what happened.
- Dependents that declare `dependsOn: ["base"]` now go through an alias
  map at plan time. Existing tests covered this; new tests in
  `CapabilityResolverTests.cs` lock the alias semantics in.
- We've partially walked back ADR 0008's "stack is in the name" public
  contract. We accept this: ADR 0008's *internal* argument (one folder
  per slice, easy comparison) is preserved; only the public surface
  changes.

## Follow-up landed

The follow-up commit on this branch ships three of the deferred items.
All three reuse the family/variant model defined above with one
additional schema field (`extends`) and one default-selection flag
(`default: true`).

- **Per-version backend folders.** `_shared/backend-aspnet/` holds the
  30 version-agnostic template files; `base/` ships only 5
  version-pinned files (`Directory.Build.props`, `Directory.Packages.props`,
  `global.json`, `Dockerfile`, `.vscode/launch.json`) and declares
  `extends: "_shared/backend-aspnet"`. The new `base-aspnet-9/` variant
  follows the same pattern — 5 .NET-9 files, same `extends`. The
  installer's overlay rule is "shared first, variant overrides on path
  conflict."
- **`default: true` flag** on `base/capability.json` so `dev-start new`
  with no version flag stays on the .NET 8 LTS rather than the highest
  version. `dev-start new --framework-version 9` switches to
  `base-aspnet-9`.
- **Cache engine swap.** `cache-memory/` (`.NET`) and
  `ts-cache-memory/` (TS) ship an `IMemoryCache` / `Map<>` implementation
  of the same `ITypedCache` / `app.cache` surface as the Redis-backed
  `cache` / `ts-cache`. Picked via the wizard or
  `dev-start add cache --engine memory`. Both pairs declare mutual
  `conflictsWith`.
- **Frontend metadata.** The existing `frontend/` capability is tagged
  `family: "frontend"`, `framework: "react"`, `frameworkVersion: "19"`,
  `provides: ["frontend"]`. The wizard fires a frontend-variant prompt
  when multiple variants exist; today there's one (React) so it's
  silent. Adding `frontend-angular/` etc. is now a metadata + files
  contribution — no CLI code changes.

## Still out of scope

- **Database engine swap** (Postgres vs MySQL vs SQLite). Same
  mechanism; ships when a real second engine is committed to.
- **Non-React frontend implementations.** Metadata is ready; the file
  set isn't.
- **`dev-start migrate`** for major framework bumps (aspnet-8 →
  aspnet-9 across a live project). The current `upgrade` handles
  minor/patch within a variant.
