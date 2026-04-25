# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/) and the project adheres to
[Semantic Versioning](https://semver.org/).

This file is managed by [release-please](https://github.com/googleapis/release-please)
from conventional-commit messages — don't edit by hand except for the
"Unreleased" section below.

## 1.0.0 (2026-04-25)


### Features

* **add:** share copy+inject pipeline between new and add, add cache capability ([c2d62d7](https://github.com/stefan-chiforiuc/dev-start/commit/c2d62d7d8f040e3f96154bd310ca0f2d06126c2d))
* capability authoring + domain tests + CHANGELOG + security workflows in generated projects ([1b73bbb](https://github.com/stefan-chiforiuc/dev-start/commit/1b73bbbb2204be704777830fb1bb5183045b9421))
* deploy-fly + deploy-aca capabilities wired to the wizard's --deploy ([0bda1e7](https://github.com/stefan-chiforiuc/dev-start/commit/0bda1e7d25eb021ec3382b9d6c0dcc2afbe437f0))
* flesh out remaining v1 capabilities + sample Orders slice ([ae932f1](https://github.com/stefan-chiforiuc/dev-start/commit/ae932f14877862f86cc87d216a0d3cc3c536f0a0))
* implement real scaffolding engine + postgres/auth/otel payloads ([ef6d3db](https://github.com/stefan-chiforiuc/dev-start/commit/ef6d3db1fd4b4837d07c96adf5f2055105aa2178))
* initial dev-start scaffold ([987aef7](https://github.com/stefan-chiforiuc/dev-start/commit/987aef7efb55a0f5ea8675d2dcb5180aab8460f0))
* injector integrity test + don't clobber existing files ([4ac5679](https://github.com/stefan-chiforiuc/dev-start/commit/4ac567911474f44ee69c748f8499f94290741a49))
* OrderPlaced → integration event bridge + dev-start list --tree ([1054425](https://github.com/stefan-chiforiuc/dev-start/commit/1054425a96ac07a5383b6d56c464348efb3fceba))
* render CLAUDE.md at generate time + order seeder + domain-event handler + list UX ([a7246ff](https://github.com/stefan-chiforiuc/dev-start/commit/a7246ffdd82733b2476ce865c89b60f5d5557a0a))
* ship .claude/settings.json + slash commands + ADR 0007 (injectors) ([62bf27a](https://github.com/stefan-chiforiuc/dev-start/commit/62bf27a3ff24ee0fb222e0f86bfd19314e7f1779))
* upgrade + doctor improvements, IAppDbContext to fix layering, k6 smoke ([5eb46fd](https://github.com/stefan-chiforiuc/dev-start/commit/5eb46fdac0c93aed75c3034828f20b7efeaf60a1))


### Bug Fixes

* .toml and .bicep weren't in the text-extension allowlist ([797688b](https://github.com/stefan-chiforiuc/dev-start/commit/797688bbbccf4165650037f08ba0e44c73e4e1ff))
* broken just up profile, missing dotnet-ef bootstrap, CLI smoke test ([c70d9a0](https://github.com/stefan-chiforiuc/dev-start/commit/c70d9a05eb8c5365fbab8607c2bc8316c3c637f2))
* CliSmokeTests InlineData can't carry string[] directly ([0fd92f6](https://github.com/stefan-chiforiuc/dev-start/commit/0fd92f63e1c1b6e0a13737eeffc1f03482bde023))
* Planner.RunAsync → synchronous Task (was async without await) ([2711495](https://github.com/stefan-chiforiuc/dev-start/commit/27114959c23182db3b98a33d2316f54330416525))
* rename .cs.fragment → .fragment (MSBuild was dropping .cs.X files) ([99d2c25](https://github.com/stefan-chiforiuc/dev-start/commit/99d2c25dd7d11628dc738ad6cfe9ae8963b313be))

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
