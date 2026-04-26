# Releasing

Two artifacts ship from this repo on independent cadences:

1. **The CLI** — published to NuGet.org as `DevStart`. Consumers install with
   `dotnet tool install -g DevStart`.
2. **The reusable CI workflow** — `.github/workflows/dotnet-ci.yml`.
   Consumers reference it by tag from generated projects.

They version separately so a CLI bump doesn't invalidate downstream workflow
pins, and vice versa.

---

## CLI release

### How a release happens

1. Commits land on `main` using
   [Conventional Commits](https://www.conventionalcommits.org/).
2. `release-please` opens (or updates) a **Release PR** that bumps
   `version.txt` and rewrites `CHANGELOG.md`.
3. Merging the Release PR triggers, in order:
   - **build** — packs the `.nupkg`, generates a CycloneDX SBOM, attests
     build provenance, uploads both as workflow artifacts.
   - **verify** — installs the just-built `.nupkg` from the local artifact
     into a clean runner, asserts `dev-start --version` matches, runs
     `dev-start new smoke-app …` end-to-end (`dotnet build` + `dotnet test`),
     runs `dev-start doctor`, and runs Trivy against the SBOM (HIGH/CRITICAL,
     unfixed ignored).
   - **deploy** — gated on the **`nuget-production` GitHub Environment**.
     Will not run until a required reviewer (currently the maintainer)
     clicks Approve. On approval, pushes the `.nupkg` to NuGet.org.

If `verify` fails, the deploy gate never opens. The `.nupkg` and SBOM stay
attached to the workflow run for inspection.

### Pre-1.0 versioning

`release-please-config.json` is set to:

- `bump-minor-pre-major: true` — `feat:` bumps `0.X.0`.
- `bump-patch-for-minor-pre-major: true` — `fix:` / `perf:` bump `0.x.Y`
  so bug fixes ship without waiting for the next feature.

### Approving a deploy

Before clicking Approve in the `nuget-production` environment, check:

- The `verify` job passed (smoke install + scaffold + build + test + doctor).
- The Trivy SBOM scan in `verify` was clean (no HIGH/CRITICAL with fixes).
- The `CHANGELOG.md` diff in the Release PR matches what you expect to ship.
- The Security tab has no fresh CodeQL alerts on the tagged commit.

### Rollback

NuGet.org does not support unpublishing. If a bad release ships:

1. Revert the offending commits on `main`.
2. Land a `fix:` (or `feat!:` if behaviour changed) describing the rollback.
3. Merge the next Release PR — this ships a higher version that supersedes
   the bad one. Consumers running `dotnet tool update -g DevStart` get the
   fix automatically.
4. If the bad release is dangerous, also
   [deprecate the broken version on nuget.org](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)
   so the listing warns consumers.

---

## Reusable workflow release

### Tagging scheme

- Immutable: `workflow-v<MAJOR>.<MINOR>.<PATCH>` (e.g. `workflow-v1.2.0`).
- Floating: `workflow-v<MAJOR>` (e.g. `workflow-v1`) — moved on every release
  in that major line.

Generated projects should pin the floating tag for unattended bumps:

```yaml
uses: stefan-chiforiuc/dev-start/.github/workflows/dotnet-ci.yml@workflow-v1
```

…or pin the immutable tag if they want to opt out of floating updates.

### Cutting a workflow release

Run the **`release-workflow`** Actions workflow manually
(`workflow_dispatch`) with the new version as input. It:

1. Validates the version string.
2. Creates an immutable annotated tag at the current `main` HEAD.
3. Force-moves the floating major tag.
4. Pushes both.

Cut a release any time the reusable workflow file changes meaningfully.
Trivial reformat / comment-only edits don't need one.

### One-time setup before CLI v1.0

Before the CLI hits `v1.0.0`, dispatch `release-workflow` once with
`version=1.0.0` so a `workflow-v1` tag exists. Without this, generated
projects that already pin `@v1` (the legacy CLI tag) will break the moment
the CLI's `v1.0.0` ships, because that tag becomes ambiguous between the CLI
and the workflow.

---

## Required GitHub setup (one-time, manual)

These cannot be created from the repo and must be set up by a repo admin:

1. **`nuget-production` environment**
   - Settings → Environments → New environment → `nuget-production`.
   - Required reviewers: `@stefan-chiforiuc` (and any future co-maintainer).
   - Environment secrets: `NUGET_API_KEY` (move it here from repo secrets,
     then delete the repo-level copy so other workflows can no longer read
     it).

2. **NuGet.org Trusted Publishing (recommended — replaces the API key)**
   - On nuget.org → Account → Trusted Publishing → register a new publisher.
   - Repository: `stefan-chiforiuc/dev-start`.
   - Workflow: `.github/workflows/release-please.yml`.
   - Environment: `nuget-production`.
   - Once registered, swap the `Push to NuGet` step in `release-please.yml`
     to use the OIDC exchange (no API key) and delete the `NUGET_API_KEY`
     secret. The deploy job already requests `id-token: write`.

3. **Branch protection on `main`**
   - Require PR review.
   - Require the `Build and test`, `Lint markdown`, `Secret scan`, and
     `Lint workflows` checks from `ci.yml` to pass before merge.
   - Disallow force-push.

4. **Action pinning hardening (follow-up)**
   - Third-party actions in `release-please.yml` and `release-workflow.yml`
     are currently pinned to major version tags. For defence in depth, pin
     them to commit SHAs once you've audited the current versions. Dependabot
     will keep SHAs current.
