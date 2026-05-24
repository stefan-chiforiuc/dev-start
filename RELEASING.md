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

### Version source of truth

`.github/release-please-manifest.json` is the **only** source of truth for
the CLI version. The packed `.nupkg` is stamped at build time via
`-p:Version=${{ needs.release-please.outputs.version }}` — no other file
should declare a version.

The first published version is **`1.0.0-alpha`**. Earlier `0.x` and `1.0.0`
references in this repo's history are pre-publication drafts that never
produced a tag, a GitHub Release, or a NuGet package.

### Pre-release graduation path

```text
0.0.0  ─►  1.0.0-alpha  ─►  1.0.0-alpha.1, .2, .3 ...  ─►  1.0.0-rc.1, .2 ...  ─►  1.0.0  ─►  1.0.x / 1.x.0 ...
```

- **alpha** — APIs may change without notice. NuGet treats it as pre-release;
  consumers only get it with `dotnet tool install -g DevStart --prerelease`.
- **rc** — feature-frozen, soak period. Bug fixes only.
- **stable** — `1.0.0`. After this, semver applies.

When ready to graduate, change `prerelease-type` (or remove `prerelease`)
in `release-please-config.json` and let release-please open the next PR.

### How a release happens

1. Commits land on `main` using
   [Conventional Commits](https://www.conventionalcommits.org/).
2. `release-please` opens (or updates) a **Release PR** that bumps
   `release-please-manifest.json` and rewrites `CHANGELOG.md`.
3. Merging the Release PR triggers, in order:
   - **build** — packs the `.nupkg`, generates a CycloneDX SBOM, attests
     build provenance, uploads both as workflow artifacts.
   - **verify** — installs the just-built `.nupkg` from the local artifact
     into a clean runner, asserts `dev-start --version` matches, runs
     `dev-start new smoke-app …` end-to-end (`dotnet build`), and runs
     Trivy against the SBOM (HIGH/CRITICAL, unfixed ignored).
   - **deploy** — gated on the **`nuget-production` GitHub Environment**.
     Will not run until a required reviewer (currently the maintainer)
     clicks Approve. On approval, pushes the `.nupkg` to NuGet.org and
     polls the v3 flat container until the version is visible.

If `verify` fails, the deploy gate never opens. The `.nupkg` and SBOM stay
attached to the workflow run for inspection.

### Pre-1.0 conventional-commit bumping

`release-please-config.json` is set to:

- `bump-minor-pre-major: true` — `feat:` bumps `0.X.0` (or the minor in `1.0.0-alpha.X`).
- `bump-patch-for-minor-pre-major: true` — `fix:` / `perf:` bump the patch
  so bug fixes ship without waiting for the next feature.

### Approving a deploy

Before clicking Approve in the `nuget-production` environment, check:

- The `verify` job passed (smoke install + scaffold + build).
- The Trivy SBOM scan in `verify` was clean (no HIGH/CRITICAL with fixes).
- The `CHANGELOG.md` diff in the Release PR matches what you expect to ship.
- The Security tab has no fresh CodeQL alerts on the tagged commit.

### Testing a release without pushing to NuGet

Three layers, cheapest first. Pick whichever matches the change you're
validating.

**Layer A — pure local.** Mirrors the CI version stamp exactly:

```bash
just install-version 1.0.0-alpha.dev
dev-start --version
dev-start new smoke-app --no-claude --with postgres auth otel
(cd smoke-app && dotnet build)
```

**Layer B — full CI dry-run.** `release-please.yml` accepts a
`workflow_dispatch` with a `dry_run` boolean input. When checked, the
workflow runs `release-please` → `build` → `verify` end to end, but the
`deploy` job is skipped (so nothing is pushed to NuGet). Use this for
workflow YAML changes, SBOM/attestation changes, or OIDC config tweaks.

Trigger from the Actions tab: **release-please → Run workflow →** check
**dry_run** → Run.

**Layer C — local release-please dry-run.** `just release-dry-run` calls
`npx release-please release-pr --dry-run` against this repo, prints the
PR title/body/files it *would* create, and exits. Requires a `GITHUB_TOKEN`
env var with read access (a fine-grained PAT is enough).

### Rollback

NuGet.org does not support unpublishing. If a bad release ships:

1. Revert the offending commits on `main`.
2. Land a `fix:` (or `feat!:` if behaviour changed) describing the rollback.
3. Merge the next Release PR — this ships a higher version that supersedes
   the bad one. Consumers running `dotnet tool update -g DevStart` get the
   fix automatically.
4. If the bad release is dangerous, also
   [deprecate the broken version on nuget.org](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)
   so the listing warns consumers, and unlist the version.

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

Before the CLI hits `v1.0.0` (graduation from `1.0.0-rc.N`), dispatch
`release-workflow` once with `version=1.0.0` so a `workflow-v1` tag exists.
Without this, generated projects that already pin `@v1` (the legacy CLI tag)
will break the moment the CLI's `v1.0.0` ships, because that tag becomes
ambiguous between the CLI and the workflow.

---

## One-time GitHub setup — `nuget-production` environment

Apply these settings in the GitHub UI exactly once. The release pipeline
(deploy job) will refuse to publish to NuGet until this environment exists
and is configured.

### 0. Repo-level workflow permissions (do this first)

Without these toggles, `release-please` fails with **"GitHub Actions is
not permitted to create or approve pull requests"** before it ever gets a
chance to open the Release PR. Job-level `permissions:` blocks cannot
override these — they are a hard repo/org gate.

Settings → **Actions** → **General** → **Workflow permissions**:

- ✓ **Read and write permissions** (the default `GITHUB_TOKEN` scope).
- ✓ **Allow GitHub Actions to create and approve pull requests**.

If the org enforces stricter defaults, the same toggles must also be on at
Org Settings → Actions → General → Workflow permissions, otherwise the repo
setting is ignored.

### 1. Create the environment

- Settings → Environments → New environment → name: `nuget-production`

### 2. Required reviewers

- Required reviewers: add `@stefan-chiforiuc` (yourself).
- Prevent self-review: **leave unchecked** (you are the sole maintainer; you
  must be able to approve your own deploys until a second maintainer joins).
- Wait timer: 0 minutes (we want fast hot-fixes; approval is the gate, not time).

### 3. Deployment branch & tag rules

- Deployment branches and tags: **Selected branches and tags**.
- Add rule: `main` (branch).
- Add rule: `v*` (tag) — for the rare case we re-trigger a deploy from a tag.

### 4. NuGet trusted publishing — no API key

The `deploy` job authenticates to NuGet.org via OIDC trusted publishing
(see the section below); there is no `NUGET_API_KEY` secret to manage.
The only configuration is a repository variable:

- `NUGET_USER` — the NuGet.org account username that owns the `DevStart`
  package. Set it under Settings → Secrets and variables → Actions →
  Variables.

If a `NUGET_API_KEY` secret still exists from an earlier setup, delete it.

### 5. Branch protection on `main`

Settings → Rules → Rulesets → New branch ruleset → name: `main-protection`.

- Target: Default branch (`main`).
- Require a pull request before merging: ✓
  - Required approvals: 1
  - Dismiss stale approvals on new commits: ✓
- Require status checks to pass: ✓
  - `Build and test` (from CI)
  - `Pack + scaffold smoke` (from CI)
  - `Verify action SHA pins` (from CI)
  - `Lint workflows` (from CI)
  - `Secret scan` (from CI)
- Require linear history: ✓ (no merge commits — squash or rebase only).
- Block force pushes: ✓.
- Restrict deletions: ✓.

### 6. Tag protection

Settings → Rules → Rulesets → New tag ruleset → name: `release-tag-protection`.

- Target: tags matching `v*` and `workflow-v*`.
- Block creation of matching tags by anyone except `github-actions[bot]`
  and the release-please-action: configure via "Bypass list".
- Block force pushes: ✓.
- Restrict deletions: ✓.

### 7. Verification

After applying, run these from a fresh terminal:

- `gh api repos/stefan-chiforiuc/dev-start/environments/nuget-production` —
  should return JSON, not 404.
- `gh api repos/stefan-chiforiuc/dev-start/actions/variables/NUGET_USER` —
  should return the NuGet account username.
- `gh api repos/stefan-chiforiuc/dev-start/rulesets` — should list
  `main-protection` and `release-tag-protection`.

### 8. Smoke-test the gate (do this before the first real release)

- Trigger the release-please workflow manually with `workflow_dispatch`
  on a throw-away tag like `v0.0.1-test`.
- Watch the deploy job pause at "waiting for approval".
- Reject the deploy → confirm nothing was pushed to NuGet.
- Delete the test tag.

### 9. Post-setup audit (every 6 months)

- Confirm reviewer list is current (no ex-maintainers).
- Confirm the `nuget-production` environment still gates the deploy job.
- Confirm the nuget.org trusted-publisher policy still matches this repo,
  workflow, and environment.

---

## NuGet.org trusted publishing (OIDC)

The `deploy` job publishes via OIDC trusted publishing — GitHub's
short-lived OIDC token is exchanged for a temporary NuGet key by the
`NuGet/login` action. No long-lived `NUGET_API_KEY` is stored anywhere.

One-time setup on nuget.org (Account → Trusted Publishing → register a
new publisher):

1. Repository: `stefan-chiforiuc/dev-start`.
2. Workflow: `.github/workflows/release-please.yml`.
3. Environment: `nuget-production`.

Then set the `NUGET_USER` repository variable (section 4) to the NuGet
account username. Until both the publisher policy and `NUGET_USER` are
in place, the deploy job's `Push to NuGet` step will fail.
