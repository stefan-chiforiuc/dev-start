# base capability

The minimum viable .NET API a project can have and still be worth using.
Every other capability depends on it.

## Wires

- Four-project layout: `{{Name}}.Api`, `{{Name}}.Application`,
  `{{Name}}.Domain`, `{{Name}}.Infrastructure`.
- Two test projects: `{{Name}}.IntegrationTests`, `{{Name}}.ArchitectureTests`.
- `Directory.Build.props` — nullable on, warnings-as-errors, central package
  management, analyzers enabled.
- `Program.cs` composition root with health checks and OpenAPI.
- `Dockerfile` (multi-stage, chiselled runtime).
- `.editorconfig`, `.gitignore`, `justfile`, `compose.yml`.
- `.devstart.json` manifest.

## Opinions

- **Minimal APIs** over controllers — see [ADR 0002](../../docs/adr/0002-net-minimal-apis.md).
- **Four projects** from day 0 even in a small app; splitting later is more
  painful than starting split.
- **Nullable on, warnings as errors.** The pain of the 99th null check is
  less than the pain of one `NullReferenceException` in prod.

## Escape hatches

- Don't want four projects? Merge `Application` and `Domain` in tiny apps,
  keep `Api` and `Infrastructure` separate. Architecture tests will need
  updates.
- Don't want warnings-as-errors? Set `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`
  in `Directory.Build.props`. You'll regret it.
