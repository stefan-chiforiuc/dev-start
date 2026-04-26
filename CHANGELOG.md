# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages — don't edit by hand except for the
"Unreleased" section below.

## [Unreleased]

### Changed

- Release pipeline split into `build` → `verify` → `deploy` stages. Deploy
  is now gated on the `nuget-production` GitHub Environment and requires
  manual reviewer approval before any push to NuGet.org. The `verify` stage
  installs the freshly packed `.nupkg` from the workflow artifact (not from
  NuGet) and runs `dev-start new` end-to-end plus a Trivy SBOM scan; if it
  fails, the deploy gate never opens.
- Reusable CI workflow (`.github/workflows/dotnet-ci.yml`) now versions
  independently from the CLI under `workflow-v<MAJOR>.<MINOR>.<PATCH>` tags
  with floating `workflow-v<MAJOR>` pointers. Cut via the new
  `release-workflow` manual dispatch. Generated projects should pin
  `@workflow-v1` instead of `@v1`. See `RELEASING.md`.
- `release-please` now bumps the patch version for `fix:` / `perf:` commits
  pre-1.0 (`bump-patch-for-minor-pre-major: true`) so bug fixes ship without
  waiting on the next feature.

### Added

- `RELEASING.md` documenting the build/verify/deploy flow, rollback
  procedure, NuGet Trusted Publishing migration path, and required one-time
  GitHub setup.
- Capability-based scaffolder: `dev-start new`, `add`, `doctor`, `upgrade`,
  `list`, `capability new`.
- v1 capabilities: `base`, `postgres`, `auth`, `otel`, `queue`, `cache`, `s3`,
  `mail`, `flags`, `sdk`, `gateway`.
- Pre-briefed `.claude/` bundle rendered at generation time with the
  installed-capabilities list and ADR index.
- Platform bundles: Docker Compose (Postgres, Keycloak, Seq, Jaeger, Mailhog,
  MinIO, RabbitMQ, Redis), devcontainer, reusable GitHub Actions workflow.
- Sample Orders slice (aggregate, value objects, domain event, CQRS handlers,
  validator, EF configuration, integration + domain tests).
- Architecture tests (NetArchTest) enforcing layering.
- k6 perf smoke script wired into the reusable CI workflow.
- Security workflows: CodeQL, Trivy filesystem scan, gitleaks, Dependabot,
  release-please with SBOM + SLSA provenance attestation on tagged releases.

### Known limitations

- `dev-start upgrade --apply` isn't implemented yet; the command diffs against
  a regenerated copy but the user applies hunks manually.
- Windows path-separator normalization for embedded resource names is
  deferred; build on Linux/WSL or Codespaces for now.
- The generated project's `dotnet test` doesn't auto-register new test
  projects in the solution file; users add them manually (`dotnet sln add`).
