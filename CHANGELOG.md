# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages — don't edit by hand except for the
"Unreleased" section below.

## [Unreleased]

### Added

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
