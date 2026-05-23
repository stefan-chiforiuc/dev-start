# `_shared/backend-aspnet`

Not a capability — a **shared overlay** used by ASP.NET-family `base-*`
variants via the `extends` field. The leading `_` excludes it from
`dev-start list` and `dev-start add`. See
[ADR 0010](../../../docs/adr/0010-flat-capability-names-and-families.md).

## What's here

The version-agnostic part of every ASP.NET base variant:

- `src/{{Name}}.Api`, `.Application`, `.Domain`, `.Infrastructure` —
  layered project skeleton, minimal API host, Problem+JSON, health
  checks, OpenAPI.
- `tests/{{Name}}.IntegrationTests`, `.ArchitectureTests` — Testcontainers
  + NetArchTest scaffolding.
- `.editorconfig`, `.gitignore`, `.env.example`, `justfile`, README.
- `.github/workflows/*` — CI, CodeQL, Trivy.
- `.vscode/extensions.json`, `tasks.json`.
- `.http/health.http`, `tests/perf/smoke.js`.

## What overlays this

Each ASP.NET base variant ships only the **version-pinned** files:

- `Directory.Build.props` — `<TargetFramework>`
- `Directory.Packages.props` — pinned NuGet versions for the target TFM
- `global.json` — SDK version pin
- `.vscode/launch.json` — `bin/Debug/netX.0/` path
- `Dockerfile` — `mcr.microsoft.com/dotnet/{sdk,aspnet}:X.0-...` tags

The installer copies the shared overlay first, then the variant's own
files; variant files win on path conflict. See
`CapabilityInstaller.BuildOverlayMap`.

## Adding a new ASP.NET version

1. Copy an existing variant: `cp -R capabilities/base capabilities/base-aspnet-N`
2. Update only the 5 version-pinned files in the new folder.
3. Set `capability.json` to `framework: "aspnet"`, `frameworkVersion: "N"`,
   `provides: ["base"]`, `extends: "_shared/backend-aspnet"`.
4. Wizard / resolver pick it up automatically — no code changes.
