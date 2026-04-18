# 0007 — Injectors, not per-capability template forks

- Status: Accepted
- Date: 2026-04-17
- Relates to: [ADR 0006](0006-capabilities-not-templates.md)

## Context

ADR 0006 locks in "capabilities compose, they don't fork." But capabilities
still need to extend shared files the base ships — `Program.cs`, each
`DependencyInjection.cs`, `appsettings.json`, `Directory.Packages.props`.
The question is how.

Options considered:

1. **Marker-based string injection.** Base files carry comment markers
   (`// devstart:api-services`). Capabilities ship fragment files plus
   a JSON spec that says "insert this fragment at marker X in file Y."
2. **Morph-based patching** (e.g. Roslyn for C#, XDocument for XML).
   Each capability ships a `Patch(project)` function that mutates the
   AST.
3. **Conditional compilation.** Every base file carries every
   capability's wiring under `#if HAS_POSTGRES` etc., and the scaffolder
   flips defines.
4. **Per-capability template forks** (the thing ADR 0006 rejected at the
   capability level — here it would mean per-file forks).

## Decision

Go with **(1) marker-based string injection**, codified as `injectors.json`
in each capability plus text fragments under `injectors/`.

## Consequences

Good:

- **Legible.** A human can read `injectors.json`, find the marker in the
  target file, and predict the output without running anything. New
  contributors review these in hours, not days.
- **Cheap to write.** No AST work, no code generator, no separate build
  step. A new capability ships three text files.
- **Composable without ordering pain.** Marker replacement concatenates
  cleanly — postgres's + auth's + otel's `AddInfrastructureServices`
  fragments all land at the same marker regardless of install order.
  The interceptor's idempotency check prevents double-application.
- **Catchable in tests.** `CapabilityIntegrityTests` asserts every
  injector references a real fragment + a real marker/anchor;
  `GeneratedSourceShapeTests` Roslyn-parses every output file.

Bad:

- **Comment markers live in your production code.** A stray `// devstart:*`
  line stays in Program.cs forever; it's harmless but cosmetically
  unusual.
- **Textual replacement is fragile.** Reformatting a base file that
  removes a marker silently breaks capability injections. We mitigate by
  testing every capability against every combination in CI and keeping
  the base files opinionated-and-stable.
- **JSON injection has to hand-code trailing commas.** The `appsettings.json`
  pattern "insert `"Redis": {...},` before `"AllowedHosts"`" produces
  valid JSON but only because every fragment ends with `,`. Tests parse
  the JSON after every install to keep us honest.

## Why not morph-based?

Roslyn-based patching would be more robust for C# but doesn't help JSON,
XML, or the dozens of format-specific files we ship. Keeping one
mechanism across all formats pays for itself in simplicity. If we hit a
real correctness problem with string injection, we'll graduate specific
injectors to morph-based — starting with `Directory.Packages.props`
(XML) or `appsettings.json`.

## Why not conditional compilation?

`#if HAS_POSTGRES` in `Program.cs` requires the scaffolder to ship
*every* capability's wiring baked into base, gated by defines. That
turns base into a god-file and means removing a capability is
impossible. Explicit opt-in via file copy beats opt-out via compile
flags.
