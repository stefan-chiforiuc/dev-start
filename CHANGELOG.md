# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages — don't edit by hand except for the
"Unreleased" section below.

## [Unreleased]

### Added — v1.1 (k8s)

- **`k8s` capability** — Helm chart + Kustomize overlays (`dev`, `stage`,
  `prod`) mirroring the compose stack, including a pre-install migration
  Job gated by the presence of a `postgres` capability and a
  `ServiceMonitor` gated by `otel`.
- **`dev-start promote <env>`** — reads `.devstart.json` and emits
  `k8s/overlays/<env>/values.generated.yaml` with per-env replica counts,
  HPA, migration, and OTel settings. `--render` shells out to
  `helm template` for a fully-rendered manifest.

### Added — v1.2 (TypeScript/Fastify stack, full parity)

- **`--stack` option on `dev-start new`** — `dotnet` (default) or
  `typescript`. Manifest persists the choice; `Planner` branches on
  `Stack` when selecting base / gateway / deploy capabilities and when
  picking the Claude briefing template.
- **Ten new TypeScript capabilities** — `ts-base`, `ts-postgres`,
  `ts-auth`, `ts-otel`, `ts-queue`, `ts-cache`, `ts-s3`, `ts-mail`,
  `ts-flags`, `ts-sdk`, `ts-gateway`, `ts-deploy-fly`, `ts-deploy-aca`.
  Fastify 5 + TypeScript strict + ESM, pnpm workspace, Vitest,
  distroless Dockerfile.
- **ADR 0008** — TS capabilities use a `ts-` prefix convention (one
  folder = one slice), with explicit `stacks: []` in each
  `capability.json` for `AddCommand` stack gating.

### Added — v1.3 (React frontend)

- **`frontend` capability** — Vite + React 19 + TanStack Router +
  TanStack Query scaffold. Stack-agnostic via the new `dependsOnByStack`
  field (targets `sdk` on .NET, `ts-sdk` on TypeScript).
- **`# devstart:web-service` marker** in the shared compose file;
  `frontend`'s injector adds a Node dev service there.
- **`Manifest.Services`** gets a `web` entry when `frontend` is
  installed; `dev-start add frontend` keeps that list in sync.

### Added — v1.4 (policy verb)

- **`dev-start policy`** with `list` / `apply` / `remove` / `validate`
  subcommands. Policy bundles ship in-repo under `policies/<name>/`
  (embedded alongside capabilities).
- **Two starter bundles** — `default-open-source` (required CodeQL /
  Trivy / gitleaks workflows, conventional-commit CI check, base-image
  allowlist) and `org-strict` (inherits + required commit signing +
  required k8s labels + stricter base-image allowlist).
- **Doctor integration** — `dev-start doctor` runs installed policies'
  validators informationally (doctor never fails); `dev-start policy
  validate` exits non-zero on any validator failure.

### Changed

- **`.mcp.json` is now fully declarative.** Capabilities declare
  `mcp: [{ name, command, args, env }]` in their `capability.json`. The
  hardcoded `WriteMcpConfig` if-blocks for `postgres` + `otel` were
  replaced by iteration over the installed capabilities' `mcp` sections.
  Output is byte-identical to v1.0 for those two capabilities, verified
  by `McpDeclarativeTests`.
- **`.claude/` bundle is stack-aware.** The template split into
  `CLAUDE.md.dotnet.template` + `CLAUDE.md.typescript.template`; skills
  live under `platform/claude/skills/{dotnet,typescript}/` and the
  Planner copies only the matching stack's set (stripping the prefix
  so the file still lands at `.claude/skills/<name>.md`).
- **Manifest schema bumped from 1 to 2** — adds `stack` (default
  `dotnet-api`) and `policies` (default `[]`). Old manifests are
  auto-migrated on load. No action required; existing v1.0 projects
  keep working.
- **`CapabilityInstaller.ApplyInjectors`** now accepts a fragment-reader
  delegate; policies reuse the same injector pipeline without
  duplicating logic.
- **`Injector` gains a `mode` field** — `"text"` (default) or
  `"json-merge"` (backed by a new `JsonMerger`). TypeScript capabilities
  use `json-merge` for `package.json` and `tsconfig.json` dependencies.
- **`Tokens`** gains `{{nameCamel}}` (camelCase) and `{{NameScope}}`
  (npm scope `@my-app`) for the TypeScript stack.
- **`doctor` gains a `tool` check kind** — generic "is X on PATH?"
  used by `k8s`, `frontend`, `ts-base` (node, pnpm, helm, kubectl, az,
  flyctl).

### Tests

- `ManifestMigrationTests`, `StackBranchingTests`, `McpDeclarativeTests`,
  `PromoteTests`, `PolicyTests`, `PolicyIntegrityTests`,
  `TsStackShapeTests`. `GeneratedSourceShapeTests` extended with `k8s`
  and `frontend` combos. `CliSmokeTests` extended with `promote` and
  `policy` help theories.

## [1.0.0] — 2026-04-18

First stable release. The CLI + capabilities + generated projects form
a coherent, self-consistent whole that can scaffold, grow with `add`,
and refresh with `upgrade --apply`.

### Added

- **CLI verbs:** `dev-start new`, `add`, `doctor`, `upgrade`, `list`,
  `capability new`.
- **Capabilities (v1 set):** `base`, `postgres`, `auth`, `otel`, `queue`,
  `cache`, `s3`, `mail`, `flags`, `sdk`, `gateway`, plus `deploy-fly` and
  `deploy-aca` for deployment targets.
- **Pre-briefed `.claude/` bundle** rendered at generation time with the
  installed-capabilities list and ADR index, plus four slash commands
  (`/bootstrap`, `/add-endpoint`, `/add-migration`, `/review-this`) and
  a curated Bash allow-list in `.claude/settings.json`.
- **Platform bundles:** Docker Compose (Postgres, Keycloak, Seq, Jaeger,
  Mailhog, MinIO, RabbitMQ, Redis), devcontainer, reusable GitHub
  Actions workflow.
- **Sample Orders slice** (aggregate, value objects, domain event, CQRS
  handlers, validator, EF `OwnsMany` config, integration + domain tests,
  seeder, integration-event bridge).
- **Architecture tests** (NetArchTest) enforcing layering.
- **k6 perf smoke** wired into the reusable CI workflow.
- **Security workflows:** CodeQL (C# + Actions), Trivy filesystem scan,
  gitleaks with a curated allowlist, Dependabot, release-please with
  SBOM (anchore/sbom-action) + SLSA provenance attestation on tagged
  releases.
- **Baseline tracking** (`.devstart/baselines.json`) with
  `upgrade --apply` 3-way merge: unmodified files get refreshed,
  user-edited files are preserved, divergent files land as
  `*.upgrade-preview` siblings.
- **Doctor `--fix`** auto-populates missing env keys into `.env.local`
  from each capability's example values.
- **Dynamic `.mcp.json`** written per-install based on which
  capabilities provide MCP servers (postgres, seq-logs).
- **Template versioning:** `.devstart.json`'s `templateVersion` is
  stamped from the CLI's assembly version on `new` and `upgrade --apply`.
- **Auto-register csproj** in `.sln` when capabilities ship new
  projects.
- **Windows support:** embedded-resource names normalised to forward
  slashes regardless of pack platform.
- **Third-party GitHub Actions pinned** to commit SHAs (with `# vN`
  comments) to address the CodeQL supply-chain rule.
- **End-to-end pack-smoke CI job** — packs the NuGet tool, installs
  it, scaffolds a project, and runs `dotnet build` from cold every PR.

### Tests

- `TokensTests`, `ManifestTests`, `CapabilityTests`,
  `CapabilityIntegrityTests`, `PlannerTests`, `AddCapabilityTests`,
  `GeneratedSourceShapeTests` (Roslyn parses every generated `.cs`,
  validates every `.csproj` as XML, every `.json` as JSON),
  `UpgradeApplyTests`, `UpgradeIntegrationTests` (covers all 6 buckets
  of the 3-way merge), `CliSmokeTests`.

### ADRs

1. Record architecture decisions.
2. Minimal APIs (not controllers).
3. EF Core + Npgsql.
4. Serilog + OpenTelemetry.
5. CQRS with MediatR + outbox.
6. Capabilities, not monolithic templates.
7. Injectors, not per-capability template forks.
