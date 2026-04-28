# Architecture

A one-page map of the `dev-start` CLI internals. Per-decision rationale
lives in [`docs/adr/`](./docs/adr); this document is the index.

## Two artifacts, two consumers

| Artifact | Source | Consumer | Cadence |
|---|---|---|---|
| The CLI (`DevStart` on NuGet) | `src/DevStart.Cli/` | Developers running `dotnet tool install -g DevStart` | Per [`RELEASING.md`](./RELEASING.md) — release-please → build → verify → deploy |
| The reusable CI workflow | `.github/workflows/dotnet-ci.yml` | Generated projects pinning `@workflow-vN` | Manual `release-workflow` dispatch |

They version independently so a CLI bump doesn't invalidate downstream
workflow pins, and vice versa.

## CLI internals

```text
src/DevStart.Cli/
├─ Program.cs                  System.CommandLine root; wires the verbs.
├─ Commands/
│  ├─ NewCommand.cs            dev-start new — invokes Planner, then
│  │                            CapabilityInstaller for each capability.
│  ├─ AddCommand.cs            dev-start add <cap> — single-capability
│  │                            install into an existing project.
│  ├─ DoctorCommand.cs         dev-start doctor — runs each installed
│  │                            capability's doctor checks; --fix
│  │                            populates missing env from examples.
│  ├─ UpgradeCommand.cs        dev-start upgrade [--apply] — diffs
│  │                            against latest templates; --apply runs
│  │                            the 3-way merge in Upgrader.cs.
│  ├─ ListCommand.cs           dev-start list [--tree] — lists
│  │                            available + installed capabilities.
│  ├─ CapabilityCommand.cs     dev-start capability new — seeds a new
│  │                            capability folder from the skeleton.
│  ├─ PromoteCommand.cs        dev-start promote <env> — emits
│  │                            k8s/overlays/<env>/values.generated.yaml.
│  └─ PolicyCommand.cs         dev-start policy list|apply|remove|validate.
│
├─ Capability.cs               Capability metadata + discovery from
│                              the embedded resource index.
├─ CapabilityInstaller.cs      Apply files + injectors to a target
│                              project. Idempotent; safe to re-run.
├─ Planner.cs                  Resolve dependsOn (and dependsOnByStack
│                              for cross-stack capabilities). Branches
│                              on Manifest.Stack to pick the right
│                              base, gateway, deploy capability, and
│                              Claude briefing template.
├─ Manifest.cs                 .devstart.json schema + migrations
│                              (v1 → v2 added Stack and Policies).
├─ Tokens.cs                   {{Name}} (PascalCase), {{name}} (kebab),
│                              {{nameCamel}} (camelCase),
│                              {{NameScope}} (npm @scope) substitution.
│                              Applied to both file paths and contents.
├─ Injector.cs                 Marker-based or anchor-based fragment
│                              insertion into existing files. Modes:
│                              "text" (default) and "json-merge"
│                              (uses JsonMerger for package.json /
│                              tsconfig.json merging).
├─ JsonMerger.cs               Tolerates jsonc comments + trailing
│                              commas in target. Output is plain JSON.
├─ Policy.cs                   Org-level policy bundles. Walks the
│                              `extends` chain bases-first; reuses
│                              the same injector pipeline as
│                              capabilities.
├─ Baselines.cs                .devstart/baselines.json — per-file
│                              hashes captured at install time, used
│                              by upgrade --apply to distinguish
│                              unmodified / user-edited / divergent.
├─ Upgrader.cs                 3-way merge for upgrade --apply.
│                              Six buckets: unchanged-unmodified,
│                              unchanged-modified, changed-unmodified
│                              (refresh), changed-modified-mergeable,
│                              changed-modified-divergent (writes
│                              .upgrade-preview), new-file, deleted-file.
├─ EmbeddedResourceIndex.cs    Shared reflection-backed index over
│                              capabilities/** and policies/**.
└─ CliVersion.cs               Reads AssemblyInformationalVersion (set
                               at pack time) and surfaces it as
                               --version.
```

### Key types and their relationships

```text
Manifest             .devstart.json on disk; loaded on every command.
  └─ Stack          dotnet | typescript
  └─ Capabilities   list of installed capability names
  └─ Policies       list of installed policy bundle names

Capability          one per capabilities/<name>/ folder, plus ts-* siblings
  └─ capability.json   metadata: name, dependsOn, dependsOnByStack,
                       conflictsWith, addsServices, envAdditions,
                       postInstall, doctor, mcp, stacks
  └─ files/            verbatim copy with token substitution
  └─ injectors.json    list of injectors (file, marker|anchor, fragment)
  └─ injectors/*.fragment

Planner.Resolve(manifest, requested)
  → walks EffectiveDependsOn (handles cross-stack frontend → ts-sdk
    when Manifest.Stack == typescript)
  → orders by topological dependency
  → returns CapabilityInstallPlan

CapabilityInstaller.Apply(plan, projectRoot)
  → CopyFiles(capability, projectRoot, tokens)
  → ApplyInjectors(capability, projectRoot, fragmentReader)
    └─ each injector is idempotent: skips if the trimmed fragment body
       is already present at the target marker/anchor
  → records baselines (file hashes) to .devstart/baselines.json
```

## How the scaffolder packs

`DevStart.Cli.csproj` declares MSBuild `<EmbeddedResource>` items that
glob `capabilities/**`, `platform/compose/**`, `platform/claude/**`,
`platform/devcontainer/**`, and `policies/**` into the `.nupkg`. The
`.csproj` does **not** carry a hard-coded `<Version>` — it is stamped
at pack time via `-p:Version=<from manifest>`.

Resource names use `%(RecursiveDir)`, which on Linux produces forward
slashes. CI runs on Linux, so the packed tool ships resources with `/`
separators. `EmbeddedResourceIndex` normalises Windows-style backslashes
on lookup so contributors building on Windows still produce a working
package.

## How a release happens

End-to-end flow lives in [`RELEASING.md`](./RELEASING.md). One-line
summary:

> push to `main` → release-please opens release PR → maintainer merges
> → **build** packs the `.nupkg`, generates SBOM, attests provenance →
> **verify** installs the packed `.nupkg` from the workflow artifact
> and runs `dev-start new` end-to-end + Trivy on the SBOM → **deploy**
> waits for manual approval in the `nuget-production` GitHub Environment
> and pushes to NuGet.org.

## ADR index

| # | Decision |
|---|---|
| [0001](docs/adr/0001-record-architecture-decisions.md) | Record architecture decisions as ADRs |
| [0002](docs/adr/0002-net-minimal-apis.md) | ASP.NET Core minimal APIs over controllers |
| [0003](docs/adr/0003-ef-core-npgsql.md) | EF Core + Npgsql over Dapper |
| [0004](docs/adr/0004-serilog-and-otel.md) | Serilog + OpenTelemetry for logs and traces |
| [0005](docs/adr/0005-cqrs-mediatr-outbox.md) | CQRS via MediatR + transactional outbox |
| [0006](docs/adr/0006-capabilities-not-templates.md) | Capabilities, not monolithic templates |
| [0007](docs/adr/0007-injectors-over-fork-templates.md) | Injectors, not per-capability template forks |
| [0008](docs/adr/0008-ts-prefix-for-typescript-capabilities.md) | `ts-` prefix for TypeScript-stack capabilities |
| [0009](docs/adr/0009-collapse-v1.1-v1.4-into-v1.0.0-alpha.md) | Collapse v1.1–v1.4 into 1.0.0-alpha |
