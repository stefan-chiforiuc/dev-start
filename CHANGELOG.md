# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages — don't edit by hand except for the
"Unreleased" section below.

The first published version will be `1.0.0-alpha`. Earlier `[1.0.0]` and
`[v0.x]` references in this project's history did not produce a tag, a
GitHub Release, or a NuGet package — they are pre-publication drafts and
have been folded into the `[Unreleased]` block below.

## [Unreleased]

### Added — CLI

- **CLI verbs:** `dev-start new`, `add`, `doctor`, `upgrade`, `list`,
  `capability new`, `promote`, `policy`.
- **Capabilities (v1 set):** `base`, `postgres`, `auth`, `otel`, `queue`,
  `cache`, `s3`, `mail`, `flags`, `sdk`, `gateway`, plus `deploy-fly` and
  `deploy-aca` for deployment targets.
- **`k8s` capability** — Helm chart + Kustomize overlays (`dev`, `stage`,
  `prod`) mirroring the compose stack, including a pre-install migration
  Job gated by the presence of a `postgres` capability and a
  `ServiceMonitor` gated by `otel`.
- **`dev-start promote <env>`** — reads `.devstart.json` and emits
  `k8s/overlays/<env>/values.generated.yaml` with per-env replica counts,
  HPA, migration, and OTel settings. `--render` shells out to
  `helm template` for a fully-rendered manifest.
- **`--stack` option on `dev-start new`** — `dotnet` (default) or
  `typescript`. Manifest persists the choice; `Planner` branches on
  `Stack` when selecting base / gateway / deploy capabilities and when
  picking the Claude briefing template.
- **Ten new TypeScript capabilities** — `ts-base`, `ts-postgres`,
  `ts-auth`, `ts-otel`, `ts-queue`, `ts-cache`, `ts-s3`, `ts-mail`,
  `ts-flags`, `ts-sdk`, `ts-gateway`, `ts-deploy-fly`, `ts-deploy-aca`.
  Fastify 5 + TypeScript strict + ESM, pnpm workspace, Vitest,
  distroless Dockerfile.
- **`frontend` capability** — Vite + React 19 + TanStack Router +
  TanStack Query scaffold. Stack-agnostic via the new `dependsOnByStack`
  field (targets `sdk` on .NET, `ts-sdk` on TypeScript).
- **`dev-start policy`** with `list` / `apply` / `remove` / `validate`
  subcommands. Policy bundles ship in-repo under `policies/<name>/`
  (embedded alongside capabilities). Two starter bundles —
  `default-open-source` and `org-strict`.
- **Doctor integration** — `dev-start doctor` runs installed policies'
  validators informationally; `dev-start policy validate` exits non-zero
  on any validator failure.
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
- **Baseline tracking** (`.devstart/baselines.json`) with
  `upgrade --apply` 3-way merge: unmodified files get refreshed,
  user-edited files are preserved, divergent files land as
  `*.upgrade-preview` siblings.

### Added — generated project

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

### Added — release pipeline

- **`RELEASING.md`** documenting the build/verify/deploy flow, rollback
  procedure (NuGet has no unpublish), NuGet Trusted Publishing migration
  path, and the one-time GitHub setup the maintainer must perform
  (environment + reviewer + secret move + branch protection).

### Changed — release pipeline

- **Release pipeline split into `release-please → build → verify → deploy`.**
  Deploy is now gated on the `nuget-production` GitHub Environment and
  requires manual reviewer approval before any push to NuGet.org. The
  `verify` stage installs the freshly packed `.nupkg` from the workflow
  artifact (not from NuGet) and runs `dev-start new` end-to-end plus a
  Trivy SBOM scan; if it fails, the deploy gate never opens. Drops
  `--skip-duplicate` so a duplicate push fails loudly. Adds a
  post-publish probe that waits for the version to appear on the NuGet
  v3 flat container before the workflow reports success.
- **`release-please` now bumps the patch version for `fix:` / `perf:`
  commits pre-1.0** (`bump-patch-for-minor-pre-major: true`) so bug
  fixes ship without waiting on the next feature.
- **`release-please-manifest.json`** is now the single source of truth
  for the package version. `version.txt` was removed.

### Changed — generated project & CLI internals

- **`.mcp.json` is now fully declarative.** Capabilities declare
  `mcp: [{ name, command, args, env }]` in their `capability.json`; the
  hardcoded `WriteMcpConfig` if-blocks were replaced by iteration over
  the installed capabilities' `mcp` sections.
- **`.claude/` bundle is stack-aware.** The template split into
  `CLAUDE.md.dotnet.template` + `CLAUDE.md.typescript.template`; skills
  live under `platform/claude/skills/{dotnet,typescript}/` and the
  Planner copies only the matching stack's set.
- **Manifest schema bumped from 1 to 2** — adds `stack` (default
  `dotnet-api`) and `policies` (default `[]`). Old manifests are
  auto-migrated on load.
- **`CapabilityInstaller.ApplyInjectors`** now accepts a fragment-reader
  delegate; policies reuse the same injector pipeline.
- **`Injector` gains a `mode` field** — `"text"` (default) or
  `"json-merge"` (backed by a new `JsonMerger`). TypeScript capabilities
  use `json-merge` for `package.json` and `tsconfig.json`.
- **`Tokens`** gains `{{nameCamel}}` (camelCase) and `{{NameScope}}`
  (npm scope `@my-app`) for the TypeScript stack.
- **`doctor` gains a `tool` check kind** — generic "is X on PATH?"
  used by `k8s`, `frontend`, `ts-base`.
- `commands/` and `agents/` in the Claude bundle are now stack-split
  into `dotnet/` and `typescript/` subfolders.
- `Capability` and `Policy` share an `EmbeddedResourceIndex` utility.
- `JsonMerger` tolerates jsonc comments and trailing commas in the
  target JSON.
- `base` and `ts-base` justfiles gain a `# devstart:just-targets`
  marker; `frontend` injects a `web` target.

### Fixed (post-review)

- **`fastify-plugin` dep** added to `ts-base/apps/api/package.json`.
- **`ts-auth /me` route** is now mounted via a new injector that
  registers `authRoutes` at the `// devstart:app-routes` marker.
- **`@fastify/jwt` JWKS wiring** rewritten to use `get-jwks` and a
  `secret` function keyed off the token's `kid`.
- **`policy extends` is now executed** — `PolicyCommand.Apply` walks the
  extends chain bases-first; `validate` and `doctor` run inherited
  validators.
- **CLAUDE templates filter by stack** — only the matching
  `CLAUDE.md.<stack>.template` is copied.
- **Transitive capability resolution** — `Planner` now walks
  `EffectiveDependsOn` so `dev-start new --stack typescript --with frontend`
  implicitly pulls in `ts-sdk`.

### Tests

- `TokensTests`, `ManifestTests`, `CapabilityTests`,
  `CapabilityIntegrityTests`, `PlannerTests`, `AddCapabilityTests`,
  `GeneratedSourceShapeTests` (Roslyn parses every generated `.cs`,
  validates every `.csproj` as XML, every `.json` as JSON),
  `UpgradeApplyTests`, `UpgradeIntegrationTests`, `CliSmokeTests`,
  `ManifestMigrationTests`, `StackBranchingTests`, `McpDeclarativeTests`,
  `PromoteTests`, `PolicyTests`, `PolicyIntegrityTests`, `JsonMergerTests`,
  `TsStackShapeTests`.

### ADRs

1. Record architecture decisions.
2. Minimal APIs (not controllers).
3. EF Core + Npgsql.
4. Serilog + OpenTelemetry.
5. CQRS with MediatR + outbox.
6. Capabilities, not monolithic templates.
7. Injectors, not per-capability template forks.
8. TypeScript capabilities use a `ts-` prefix convention with explicit
   `stacks: []` for `AddCommand` stack gating.
