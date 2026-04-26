# Roadmap

This roadmap is intentionally narrow. We'd rather do one thing excellently
than ten things badly.

The capability registry — what exists today vs. what is planned — lives in
[`capabilities/README.md`](./capabilities/README.md). The release process
and the v1 cut criteria live in [`RELEASING.md`](./RELEASING.md).

## v1 — `.NET API starter + capabilities`

The baseline release. Acceptance test from the design doc:

- **Path A (Codespaces):** template → "Open in Codespaces" → < 60 s to a
  running stack, seeded DB, green tests.
- **Path B (local):** `dotnet tool install -g DevStart` → `dev-start new` →
  `just up` → < 2 min to same result.
- `dev-start add <cap>` works idempotently for every v1 capability.
- `dev-start doctor` and `dev-start upgrade` pass smoke tests.
- Generated `.claude/` bundle answers `/add-endpoint <name>` with a full
  slice (aggregate, handler, migration, test) respecting house style.
- CI includes CodeQL, Trivy, gitleaks, architecture tests, API-diff, k6
  perf smoke.
- Release pipeline signs containers (cosign), attaches SBOM (CycloneDX),
  produces SLSA L2 attestation.

The full v1 cut checklist is in [`RELEASING.md`](./RELEASING.md#v1-cut-criteria).

## v1.1 — `k8s capability`

- `dev-start add k8s` → Helm chart + Kustomize overlays (`dev`, `stage`,
  `prod`) mirroring the compose stack.
- A "promote" command that reads `.devstart.json` and writes matching
  manifests (service count, ports, env bindings, migration job).

## v1.2 — second stack: TypeScript + Fastify

- Validates the "capabilities are reusable across stacks" hypothesis.
- Shares module shapes for `auth`, `otel`, `queue`, `sdk`, `flags`.
- If the abstraction doesn't hold up, we rework it here rather than
  locking it in later.

## v1.3 — frontend companion

- Optional React + TanStack Query starter that consumes the generated
  TypeScript SDK from `sdk` capability.
- Intentionally loose coupling: any frontend can consume the SDK.

## v1.4 — `dev-start policy`

- Org-level policy bundles that layer on top of the OSS defaults:
  required commit signing, mandatory base-image allowlist, required
  k8s labels, required log fields, etc.
- Enforced in CI via the reusable workflow.

## Explicitly not planned

- Per-stack wizard forks beyond what fits in 5 questions.
- Mobile, desktop, PWA scaffolds (different tool).
- A plugin API before the capabilities list settles.
- Multi-cloud deploy generators beyond Fly.io + Azure Container Apps in v1.
  (k8s from v1.1 covers most real needs.)

Changes to this roadmap require an ADR.
