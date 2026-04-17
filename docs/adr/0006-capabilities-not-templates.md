# 6. Capabilities, not monolithic templates

Date: 2026-04-17
Status: Accepted

## Context

Existing scaffolders (Yeoman, `dotnet new`, Cookiecutter) generate a
project once and forget about it. Over time, the generated code drifts
from the template: best practices change, dependencies get CVEs,
idioms evolve. Users end up maintaining a snapshot of a 2-year-old
template.

We want `dev-start` to stay useful for the life of the project, not
just day 0.

## Decision

- The unit of composition is a **capability module**: a small,
  self-contained folder under `capabilities/` describing what it
  changes and how.
- `dev-start new` is equivalent to `dev-start add base` followed by
  the user's selected capabilities.
- Every capability mutation updates `.devstart.json` in the target
  project, so the tool always knows what's installed.
- `dev-start upgrade` compares the target's `.devstart.json` against
  the latest capability versions and proposes a PR with the delta.

Each capability provides:

- `capability.json` — name, version, dependencies, description.
- `README.md` — what it wires, opinionated choices, escape hatches.
- `files/` — files to copy into the target project.
- `patches/` — structured edits to existing files (composition root,
  compose, justfile, CI).

## Consequences

- No duplication between "day 0" and "day 30" code paths.
- Upgrade is a first-class operation, not a rewrite.
- Capability authors must write idempotent patches — harder than
  copy-paste, cheaper than maintaining N templates.
- Future stacks (TS + Fastify, Go) can reuse capability shapes;
  `capability.json` is language-agnostic.

## Alternatives considered

- **`dotnet new` template pack alone**: ships one-shot scaffolding; no
  story for day-30 additions or upgrades.
- **Cookiecutter-style string substitution**: works, but doesn't track
  what was installed, so upgrades must be manual diffs.
- **Plugin API**: attractive but premature. We'll consider one once
  the built-in capabilities list has settled.
