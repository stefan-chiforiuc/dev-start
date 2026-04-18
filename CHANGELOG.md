# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages — don't edit by hand except for the
"Unreleased" section below.

## [Unreleased]

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
