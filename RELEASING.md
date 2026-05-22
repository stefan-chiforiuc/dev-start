# Releasing

Single source of truth for how `dev-start` ships. Operational runbook,
gates, and v1 cut criteria all live here.

## Cadence

**Event-driven.** A release happens when the maintainer merges the
release-please PR. There is no fixed rhythm. Pre-v1 every `feat:` bumps
the minor; pre-v1 `fix:` ships in the next minor (no patch bumps in 0.x,
per `.github/release-please-config.json`).

## How a release happens (end-to-end)

```text
                  feat:/fix: commits
                          │
                          ▼
                ┌──────────────────┐
                │  push to main    │
                └────────┬─────────┘
                         │ release-please.yml
                         ▼
              ┌────────────────────────┐
              │  Release PR open/upd.  │  ← maintainer reviews
              └────────────┬───────────┘
                           │ merge
                           ▼
              ┌────────────────────────┐
              │  tag vX.Y.Z + GitHub   │
              │   Release (no assets)  │
              └────────────┬───────────┘
                           │ release: published
                           ▼
              ┌────────────────────────┐
              │  release-build.yml     │
              │   pack + SBOM +        │
              │   attest + smoke       │
              │   attach artifacts to  │
              │   the GH Release       │
              └────────────┬───────────┘
                           │ green
                           ▼
              ┌────────────────────────┐
              │  release-publish.yml   │  ← workflow_dispatch
              │   (manual click)       │     by maintainer
              │   OIDC → NuGet push    │
              │   mark Release latest  │
              └────────────────────────┘
```

Three workflows; one button click per release. The button exists so
that if the smoke fails or the maintainer spots a problem in the
attached artifacts, NuGet stays untouched. A bad tag and a bad
GitHub Release are both cheap to delete; a bad NuGet listing is
forever (you can only unlist).

## The single button click — what to check before pushing it

When `release-build.yml` finishes green, open the GitHub Release for
the new tag and confirm:

- The packed `.nupkg` is attached.
- The CycloneDX SBOM is attached.
- The build-provenance attestation is recorded (`gh attestation list`).
- The release notes look right.
- The smoke job logs show `dev-start --version` printing the expected
  version and `dev-start new` succeeding against a tmpdir.

Then run the **`release-publish`** workflow from the Actions tab,
passing the tag (e.g. `v0.4.0`) as input.

## NuGet trusted publishing (OIDC)

`dev-start` does not store a `NUGET_API_KEY`. Publishing is brokered
via GitHub OIDC trusted publishing.

One-time setup on nuget.org (manual, not in code):

1. Sign in at <https://www.nuget.org/>.
2. Open the `DevStart` package → **Manage Owners / Trusted Publishing**.
3. Add a trusted publisher:
   - Repository owner: `stefan-chiforiuc`
   - Repository: `dev-start`
   - Workflow filename: `release-publish.yml`
   - Environment: *(leave blank)*
4. Save.

`release-publish.yml` requests an OIDC token at runtime and exchanges
it for a short-lived NuGet API key. Nothing to rotate.

## Yank / rollback

NuGet does not support deletion, only **unlist**. Unlisting hides the
package from search but existing installs still work.

1. Sign in at nuget.org → `DevStart` → version `vX.Y.Z` → **Unlist**.
2. In the repo, push a `fix:` commit that addresses the underlying issue.
   Release-please will open a Release PR for the next version.
3. Roll forward. Do not attempt to re-publish a previously-published
   version number — NuGet rejects duplicates and `--skip-duplicate`
   would silently no-op.
4. Add a note to the release that supersedes the bad one explaining
   why the previous version was unlisted.
5. File an issue using the post-incident template (below).

## Hotfix procedure

**While pre-v1 (single trunk):**

- Land the `fix:` on `main`. Release-please will open a Release PR
  immediately.
- Merge the Release PR; the standard pipeline runs.
- No cherry-picking needed because there is no supported back-version.

**After v1 (two-line model):**

- Land the `fix:` on `main`.
- Cherry-pick onto `release/0.x` (where 0.x is the supported minor).
- A second release-please instance configured against
  `.github/release-please-config.0x.json` opens a Release PR on
  `release/0.x`. Merge it; the same split build/publish runs.

## `release/0.x` branch policy (v1 onward)

- Forked from the final 0.x tag at the moment v1.0.0 ships.
- Receives **security fixes only** (`fix:` commits with a `security:`
  scope or a `BREAKING CHANGE:` footer that documents the CVE).
- Supported for **6 months** after v1, matching `SECURITY.md`.
- Same OIDC trusted publishing; the trusted-publisher policy on
  nuget.org allows both `main` and `release/0.x` as branch patterns.

## v1 cut criteria

To declare `v1.0.0`, **all** of the following must be true.

### Functional

- [ ] Path A (Codespaces): template → "Open in Codespaces" → < 60 s to
      a running stack + green tests, verified on a real Codespace
      within 7 days of cut.
- [ ] Path B (local): `dotnet tool install -g DevStart` →
      `dev-start new` → `just up` → < 2 min to same result, on macOS,
      Linux, and WSL.
- [ ] `dev-start add <cap>` is idempotent for every v1 capability
      (`base`, `postgres`, `auth`, `otel`, `queue`, `cache`, `s3`,
      `mail`, `sdk`, `flags`, `gateway`, `deploy-fly`, `deploy-aca`).
- [ ] `dev-start doctor` reports no false positives on a freshly-
      generated default project.
- [ ] `dev-start upgrade --apply` works (currently flagged as not
      implemented).
- [ ] `.claude/` bundle's `/add-endpoint <name>` produces a buildable,
      test-passing slice on a freshly-generated project.

### Quality

- [ ] Zero open issues labelled `release-blocker`.
- [ ] CI green on `main` for 7 consecutive days.
- [ ] Architecture tests green; no `[Skip]`s outside documented
      limitations.
- [ ] CodeQL + Trivy + gitleaks clean on `main`.

### Supply chain

- [ ] OIDC trusted publishing configured on nuget.org (no
      `NUGET_API_KEY` in repo secrets).
- [ ] All third-party Actions pinned to commit SHAs.
- [ ] SBOM (CycloneDX) attached to the GH Release.
- [ ] Build provenance attestation present and verifiable with
      `gh attestation verify`.

### Docs

- [ ] `RELEASING.md` reflects current reality.
- [ ] `SECURITY.md` lists a real reporting inbox (currently TODO).
- [ ] `docs/when-to-leave-the-road.md` covers every default a sensible
      team might need to deviate from.
- [ ] No `(TODO:)` markers anywhere in user-facing docs.

### Operational

- [ ] `release/0.x` branch created from the final 0.x tag.
- [ ] First production-shape release through the split workflow has
      succeeded at least once on a 0.x.

## Documentation — single source of truth

| Topic | Canonical home | Linked from |
|---|---|---|
| Quickstart, two paths, CLI verbs | `docs/golden-path.md` | `README.md` |
| Opinions / paved road | `docs/paved-road.md` | `README.md` |
| Escape hatches | `docs/when-to-leave-the-road.md` | `README.md`, capability READMEs |
| Capability registry + authoring | `capabilities/README.md` | `README.md`, `ROADMAP.md` |
| Branching, commits, reviews, ADRs | `docs/way-of-working.md` | `CONTRIBUTING.md` |
| Release process + v1 cut | `RELEASING.md` (this file) | `README.md`, `CONTRIBUTING.md`, `docs/way-of-working.md`, `SECURITY.md` |
| Vulnerability reporting + posture | `SECURITY.md` | `README.md` |
| Architecture decisions | `docs/adr/` | `docs/paved-road.md`, capability READMEs |

If you change one of these topics, change it in its canonical home and
nowhere else. PRs that duplicate content into a second place will be
sent back.

## Post-incident template

Open an issue titled `post-incident: <release> — <one-line summary>`
with:

```text
**What happened**
<one paragraph>

**Impact**
- Versions affected:
- Users affected (estimate):
- Time-to-detect:
- Time-to-mitigate (unlist or fix):

**Root cause**
<one paragraph>

**Why our gates didn't catch it**
<smoke job? CI? human review? supply-chain step?>

**What we're changing**
- [ ] <gate / test / doc change>
- [ ] <ADR if a default needs to shift>
```
