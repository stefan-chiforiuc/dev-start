# Way of working

This is how the `dev-start` maintainers work on `dev-start`. It's also a
reasonable default for projects generated from it.

## Branching

- `main` is always releasable.
- Feature branches are short-lived: `feat/<ticket>-<slug>`,
  `fix/<ticket>-<slug>`.
- **No long-lived develop/release branches.** Release-please cuts releases
  from `main` on merge.

## Commits

- **Conventional commits.** `feat:`, `fix:`, `docs:`, `refactor:`, `test:`,
  `chore:`, `ci:`, `perf:`, `deps:`.
- Scope when useful: `feat(postgres): add row-level security helper`.
- Breaking changes: footer `BREAKING CHANGE: <what>`; plus an ADR.
- Squash-merge PRs, preserving the conventional-commit title.

## Reviews

- Every PR needs one reviewer listed in `CODEOWNERS`.
- Reviewers check: tests, architecture rules, ADR coverage of any default
  change, docs for user-visible changes.
- **No nits after approval.** If you spotted a nit during review, leave
  it as a non-blocking comment.

## Releases

The release pipeline is documented end-to-end in
[`RELEASING.md`](../RELEASING.md). Summary:

- Release-please opens a release PR on every merge to `main`.
- Merging the release PR triggers `build` → `verify` (automated) → `deploy`
  (manual approval in the `nuget-production` GitHub Environment). Deploy
  pushes to NuGet only after the maintainer approves.
- Pre-1.0: `bump-patch-for-minor-pre-major: true`, so `fix:` and `perf:`
  ship patches without waiting for the next feature.
- Patch: any `fix:` / `perf:` merged to `main`.
- Minor: any `feat:` or `deps:` group update.
- Major: any `BREAKING CHANGE:`.

## Hotfixes

- Same process as a normal fix. Release-please handles the version bump.
- If a hotfix must ship faster than the next release PR, we cherry-pick
  the fix onto the previous release branch, tag it manually, and
  document in the next release PR.

## On-call / triage

Weekly rotation (once there's more than one maintainer). The on-call
person is responsible for:

- Triage new issues within 2 business days.
- Review Dependabot / Renovate PRs.
- Address security findings from CodeQL / Trivy / gitleaks.

## ADRs

- One per non-trivial default.
- Lightweight format: context, decision, consequences.
- Numbered in order; never renumbered. Superseded ADRs link to the new
  one.
- A PR that changes an existing default without an ADR gets sent back.

## Tests

- No PR merges red. If the failure is flaky, we fix the flake first.
- **Perf smoke budget** is a hard gate. If the budget breaks, the fix is
  either an optimisation or a budget-change ADR — not silencing the test.

## Documentation

- Anything user-facing lives in `docs/` or a capability's `README.md`.
- We don't ship feature docs only in release notes.
- The README's "Two paths to running code" section must stay executable
  at every release. CI runs it on a schedule.
