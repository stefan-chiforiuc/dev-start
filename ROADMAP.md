# Roadmap

This roadmap is intentionally narrow. We'd rather do one thing excellently
than ten things badly.

## v1 — `.NET API starter + capabilities` (shipping in 1.0.0-alpha)

The baseline release. Acceptance test from the design doc:

- **Path A (Codespaces):** template → "Open in Codespaces" → < 60 s to a
  running stack, seeded DB, green tests.
- **Path B (local):** `dotnet tool install -g DevStart` → `dev-start new` →
  `just up` → < 2 min to same result.
- `dev-start add <cap>` works idempotently for: `postgres`, `auth`, `otel`,
  `queue`, `cache`, `s3`, `mail`, `sdk`, `flags`, `gateway`.
- `dev-start doctor` and `dev-start upgrade` pass smoke tests.
- Generated `.claude/` bundle answers `/add-endpoint <name>` with a full
  slice (aggregate, handler, migration, test) respecting house style.
- CI includes CodeQL, Trivy, gitleaks, architecture tests, API-diff, k6
  perf smoke.
- Release pipeline signs containers (cosign), attaches SBOM (CycloneDX),
  produces SLSA L2 attestation.

## Folded into 1.0.0-alpha (originally scoped as v1.1–v1.4)

The items below were originally planned as separate v1.1, v1.2, v1.3, and
v1.4 releases. Before any version had actually been published to NuGet,
they were consolidated into the first release as a single coherent MVP. See
[ADR 0009](./docs/adr/0009-collapse-v1.1-v1.4-into-v1.0.0-alpha.md) for the
rationale, and `CHANGELOG.md` for the concrete changes.

### k8s capability + `promote` verb

- `dev-start add k8s` → Helm chart + Kustomize overlays (`dev`, `stage`,
  `prod`) mirroring the compose stack.
- `dev-start promote <env>` reads `.devstart.json` and writes matching
  manifest values (service count, ports, env bindings, migration job).

### Second stack: TypeScript + Fastify

- `dev-start new --stack typescript` — Fastify + TS skeleton.
- Ten `ts-*` capabilities at parity with the .NET set; shared module
  shapes for `auth`, `otel`, `queue`, `sdk`, `flags`. The abstraction
  held up; no rework needed.

### Frontend companion

- Optional React + TanStack Query starter via `dev-start add frontend`.
  Consumes the generated TS SDK on the TS stack, or the generated .NET
  SDK on the .NET stack. Intentionally loose coupling: any frontend can
  consume the SDK.

### `dev-start policy`

- Org-level policy bundles ship in-repo and layer on top of the OSS
  defaults: required CodeQL/Trivy/gitleaks workflows, base-image
  allowlist, required k8s labels, etc. Two starter bundles ship:
  `default-open-source` and `org-strict`.
- Enforced in CI by calling `dev-start policy validate`.

## Explicitly not planned

- Per-stack wizard forks beyond what fits in 5 questions.
- Mobile, desktop, PWA scaffolds (different tool).
- A plugin API before the capabilities list settles.
- Multi-cloud deploy generators beyond Fly.io + Azure Container Apps in v1.
  (`k8s` capability covers most real needs.)

Changes to this roadmap require an ADR.
