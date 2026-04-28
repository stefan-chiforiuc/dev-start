# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages. **Don't hand-edit released sections**
(`## [1.x.y]` blocks) — they're rewritten on every release. The
`[Unreleased]` block below is allowed for hand-written notes that get
**absorbed** into the next version's section when release-please cuts the
next release PR; treat them as a draft, not a permanent home.

The first published version will be `1.0.0-alpha`. Earlier `[1.0.0]` and
`[v0.x]` references in this project's history did not produce a tag, a
GitHub Release, or a NuGet package — they are pre-publication drafts and
have been folded into the `[Unreleased]` block below.

## 1.0.0-alpha (2026-04-28)


### Features

* .mcp.json is generated dynamically from installed capabilities ([#17](https://github.com/stefan-chiforiuc/dev-start/issues/17)) ([33d4141](https://github.com/stefan-chiforiuc/dev-start/commit/33d4141f755478f174fb5f85fe7dc16b8add9fe5))
* auto-register new csproj files in the solution ([#10](https://github.com/stefan-chiforiuc/dev-start/issues/10)) ([e79ca0a](https://github.com/stefan-chiforiuc/dev-start/commit/e79ca0a89f14b5b067e8b5ee9cd8f1c74a6de929))
* backport v1.1-v1.4 roadmap into 1.1.0 (k8s, TS stack, frontend, policy) ([8e8d169](https://github.com/stefan-chiforiuc/dev-start/commit/8e8d16980bf17bc0efededa22f82489257599f12))
* backport v1.1–v1.4 roadmap into 1.1.0 (k8s, TS stack, frontend, policy) ([fdb129f](https://github.com/stefan-chiforiuc/dev-start/commit/fdb129ff8f0b960637ccfc09ddcaab15fa59bfe2))
* dev-start doctor --fix auto-populates missing env keys ([#16](https://github.com/stefan-chiforiuc/dev-start/issues/16)) ([12bb93e](https://github.com/stefan-chiforiuc/dev-start/commit/12bb93ebf2167e2f0dd7fcd6b3079de97ff2ca5e))
* dev-start upgrade --apply with baseline tracking ([#9](https://github.com/stefan-chiforiuc/dev-start/issues/9)) ([6a3b342](https://github.com/stefan-chiforiuc/dev-start/commit/6a3b342bab9483e4649ba83b4deeadde4d0d0686))
* initial dev-start scaffold ([987aef7](https://github.com/stefan-chiforiuc/dev-start/commit/987aef7efb55a0f5ea8675d2dcb5180aab8460f0))
* normalize Windows backslash paths in resource lookups ([#11](https://github.com/stefan-chiforiuc/dev-start/issues/11)) ([0e4c542](https://github.com/stefan-chiforiuc/dev-start/commit/0e4c542098935e96aef390531c73ccbb9862e8c3))
* stamp .devstart.json templateVersion from the CLI assembly ([#13](https://github.com/stefan-chiforiuc/dev-start/issues/13)) ([44b5e36](https://github.com/stefan-chiforiuc/dev-start/commit/44b5e36f93987eadf61a35910038e2ee803c701e))


### Bug Fixes

* address post-review bugs + design improvements (1.1.0) ([9014d6e](https://github.com/stefan-chiforiuc/dev-start/commit/9014d6e246acc59d0b90b5afef5f56677412a029))


### Documentation

* add ADR 0009, ARCHITECTURE.md, SUPPORT.md ([0923a1f](https://github.com/stefan-chiforiuc/dev-start/commit/0923a1fa3be1be2c9eaac359701abac641be7879))
* add v1.1-v1.4 backport plan folding k8s, TS stack, frontend, and policy verb into 1.1.0 ([902386e](https://github.com/stefan-chiforiuc/dev-start/commit/902386e8411aa5745320a7a984afdc8333fb1b5e))
* align all docs to 1.0.0-alpha + add install-hooks ([4f208b3](https://github.com/stefan-chiforiuc/dev-start/commit/4f208b389b2f9bc066cc68c70333422bf01bced9))
* retire stale planning content ([4902cb6](https://github.com/stefan-chiforiuc/dev-start/commit/4902cb675865932669f08e30c3e3431a5d6b7e58))

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
