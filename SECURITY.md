# Security policy

## Supported versions

`dev-start` is **pre-v1**. Only the `main` branch is supported. Once v1
ships, the latest minor will be supported with security fixes for at least
6 months via the `release/0.x` branch (see [`RELEASING.md`](./RELEASING.md)).

## Reporting a vulnerability

**Do not file a public issue for security problems.**

Use GitHub's **Private Vulnerability Reporting** on this repository, or
email `security@dev-start.dev` (TODO: real inbox once domain is set up).

Please include:

- A description of the issue and its impact.
- Steps to reproduce or a proof-of-concept.
- The commit SHA or release tag you tested against.
- Your disclosure timeline, if any.

### Response SLA

- **Acknowledgement**: within 3 business days.
- **Triage + severity assessment**: within 10 business days.
- **Fix or mitigation**: target 30 days for High/Critical, 90 days for
  Medium.
- **Coordinated disclosure**: we aim to publish a CVE and advisory at the
  same time as the fix release.

## What we care about

Because `dev-start` is a scaffolder, the threat model includes both the
tool itself *and* the defaults it embeds into generated projects. We treat
the following as in-scope:

- Command injection / path traversal in the CLI.
- Unsafe defaults in generated code (secret logging, missing auth,
  dependency-confusion-prone package references).
- Supply-chain: unsigned artifacts, missing SBOM, compromised
  base-image pins.
- Template-level misconfig in CI workflows we ship (e.g. `pull_request_target`
  misuse).

Out of scope:

- Vulnerabilities in upstream dependencies that we track via Dependabot /
  Renovate (report those upstream).
- Users of the generated templates who remove or disable the shipped
  security gates.

## Our own posture

The `dev-start` package itself:

- Published to NuGet via OIDC trusted publishing (no long-lived API key).
- SBOM (CycloneDX) attached to every GitHub Release.
- SLSA-style build provenance via `actions/attest-build-provenance`.
- CodeQL + Trivy + gitleaks gates on every PR; `actionlint` on workflows.
- Pre-commit `gitleaks` for contributors.

The posture defaults shipped *into generated projects* (cosign-signed
container images, CodeQL/Trivy on user PRs, k6 perf smoke, etc.) live in
[`docs/paved-road.md`](./docs/paved-road.md).
