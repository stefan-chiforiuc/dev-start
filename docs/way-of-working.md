# Way of working

This is how the `dev-start` maintainers work on `dev-start`. It's also a
reasonable default for projects generated from it.

## Branching

- `main` is always releasable.
- Feature branches are short-lived: `feat/<ticket>-<slug>`,
  `fix/<ticket>-<slug>`.
- **No long-lived develop/release branches** while pre-v1. A
  `release/0.x` branch is forked at the v1 cut to receive security
  backports — see [`RELEASING.md`](../RELEASING.md).

## Commits

- **Conventional commits.** `feat:`, `fix:`, `docs:`, `refactor:`, `test:`,
  `chore:`, `ci:`, `perf:`, `deps:`.
- Scope when useful: `feat(postgres): add row-level security helper`.
- Breaking changes: footer `BREAKING CHANGE: <what>`; plus an ADR.
- Squash-merge PRs, preserving the conventional-commit title.

## Reviews

While the project has a single maintainer, there is no code-review gate;
the maintainer self-reviews and merges. A peer-review policy will be
re-introduced when a co-maintainer joins.

## Releases

See [`RELEASING.md`](../RELEASING.md) for the full release process,
gates, and runbook.

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
- Every topic has a single source of truth; other places link to it.
  See the SoT table in [`RELEASING.md`](../RELEASING.md#documentation).
