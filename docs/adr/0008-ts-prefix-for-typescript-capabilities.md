# 0008 — `ts-` prefix for TypeScript-stack capabilities

- Status: Accepted
- Date: 2026-04-24
- Relates to: [ADR 0006](0006-capabilities-not-templates.md), [ADR 0007](0007-injectors-over-fork-templates.md)

## Context

The 1.0.0-alpha scope includes a second stack alongside .NET:
TypeScript / Fastify (originally planned as v1.2 — see
[ADR 0009](0009-collapse-v1.1-v1.4-into-v1.0.0-alpha.md) for why it was
consolidated). The question is how to represent stack-specific
capabilities in the capabilities tree.

Options:

1. **Prefix naming** — `ts-base`, `ts-postgres`, `ts-auth`, … live as
   sibling folders to `base`, `postgres`, `auth`. One folder = one
   slice. `AddCommand` rejects installation into a mismatched stack.
2. **One capability, stack branching inside** — a single `postgres/`
   folder with `files/dotnet/` + `files/typescript/` subfolders and a
   branch in `CapabilityInstaller.CopyFiles`.
3. **Separate capability trees per stack** — `capabilities/dotnet/*` and
   `capabilities/typescript/*`.

## Decision

Go with **(1) `ts-` prefix**.

Capabilities targeting a stack declare it explicitly via
`"stacks": ["typescript-fastify"]` in `capability.json`. A stack-agnostic
capability (like `frontend`) declares `dependsOnByStack` instead so its
dependencies follow the current project stack.

## Consequences

Good:

- **One folder per slice** — reading a capability's implementation is
  still "look at this directory." No mental bookkeeping of "which
  subfolder applies."
- **`dev-start list` is legible** — `ts-auth` obviously pairs with
  `ts-base`, and a new contributor can guess the naming of future
  additions.
- **The stack gate is a one-liner** — `AddCommand` checks
  `cap.Stacks.Contains(manifest.Stack)` (or the prefix convention) and
  errors cleanly. No runtime polymorphism.
- **Shared infra stays shared** — when two stacks legitimately want the
  same MCP server (`postgres` MCP, `seq-logs`), both `postgres` and
  `ts-postgres` declare the identical `mcp` block. The declarative
  `mcp` refactor makes this symmetric.

Bad:

- **Name duplication** — `postgres` and `ts-postgres` are two entries in
  `dev-start list`. Users need to know which stack they're on. We mitigate
  by stamping the stack on `dev-start new` output and in the Claude
  briefing.
- **Cross-stack slices need a convention** — `frontend` isn't `ts-`
  prefixed because it's cross-stack. We accept the small inconsistency
  over the alternative (`frontend` and `dotnet-frontend`, which would
  imply a hypothetical second variant that isn't coming).

## Why not "one capability, stack branching inside"?

Breaks ADR 0006's "one folder = one slice." A single `postgres/capability.json`
with `files/dotnet/`, `files/typescript/` forces every code path that
reads capability files (`CapabilityInstaller`, `Upgrader`,
`GeneratedSourceShapeTests`) to branch on stack. The branching pays off
only when the implementations are _mostly_ identical; they aren't —
EF Core and Kysely have almost nothing in common at the file level.

## Why not separate trees per stack?

`capabilities/dotnet/`, `capabilities/typescript/` hides the symmetry —
you'd scroll through two sibling trees to compare "what does postgres
look like on each stack." Flat + prefixed wins for discoverability.
