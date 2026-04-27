# 0009 — Collapse v1.1–v1.4 into 1.0.0-alpha

- Status: Accepted
- Date: 2026-04-27
- Relates to: [ADR 0006](0006-capabilities-not-templates.md), [ADR 0008](0008-ts-prefix-for-typescript-capabilities.md)
- Supersedes: the earlier roadmap structure documented in `ROADMAP.md` v1.1 / v1.2 / v1.3 / v1.4 sections

## Context

The original roadmap planned four post-1.0 releases as separate cuts:

- **v1.1** — `k8s` capability + `dev-start promote <env>`.
- **v1.2** — TypeScript / Fastify second stack with the `ts-*` family.
- **v1.3** — `frontend` capability (Vite + React + TanStack), cross-stack.
- **v1.4** — `dev-start policy` verb + `default-open-source` /
  `org-strict` policy bundles.

Before any version reached NuGet, the v1.0 release attempt stalled
(release-please opened the PR, the manifest bumped, but the publish step
never produced a tag, a GitHub Release, or a NuGet package — see
[`RELEASING.md`](../../RELEASING.md) and the `[1.0.0]` section that was
later folded into `[Unreleased]` in `CHANGELOG.md`). Meanwhile, the v1.1
through v1.4 work was implemented on a parallel branch
(`claude/plan-backport-features-j7RvT`) and merged to `main` as a single
backport PR (#25) titled *"feat: backport v1.1–v1.4 roadmap into 1.1.0"*.

That left the project in a confused state: a "ghost 1.0.0" that never
shipped, a `[Unreleased]` block describing four phantom future releases,
and three sources of truth disagreeing on the version
(`release-please-manifest.json: 0.1.0`, `version.txt: 0.2.0`,
`README: 1.1.0 (unreleased)`).

## Decision

Ship v1.1 through v1.4 together as the **first published version**, named
`1.0.0-alpha`. Do not retroactively claim a 1.0.0 was cut. Treat the prior
`[1.0.0] — 2026-04-18` CHANGELOG entry as a pre-publication draft and
fold it into `[Unreleased]`.

Version progression for the next several releases:

```
0.0.0  ─►  1.0.0-alpha  ─►  1.0.0-alpha.1, .2, .3 ...  ─►  1.0.0-rc.1, .2 ...  ─►  1.0.0  ─►  1.0.x / 1.x.0 ...
```

`release-please-manifest.json: 0.0.0` is the only source of truth for the
package version. `release-please-config.json` carries
`release-as: 1.0.0-alpha`, `prerelease: true`, `prerelease-type: alpha`
so the next release PR proposes `1.0.0-alpha`.

## Why consolidate rather than ship four releases

1. **Nothing has shipped yet.** "Backwards compatibility" with v1.0 isn't
   a constraint, because v1.0 never reached a single user. Phasing
   features over four releases buys nothing when there are no consumers
   to manage migration for.

2. **The v1.1–v1.4 features are coupled in practice.** `frontend`
   (planned for v1.3) consumes the SDK from either `sdk` (.NET) or
   `ts-sdk` (v1.2). `policy` (v1.4) wraps capability + workflow +
   compose surfaces that the TypeScript stack expanded. Shipping any of
   them without the others produces a coherent product only by accident.
   The 1.0.0-alpha bundle is *the* MVP; pieces of it shipped alone would
   be rough drafts.

3. **An alpha is the honest framing.** The original v1.0 plan promised
   "stable surface". That promise was unwarranted: the CLI surface
   (`new`, `add`, `doctor`, `upgrade`, `promote`, `policy`) is broad,
   the manifest schema has already migrated once (v1 → v2), and the
   capability authoring contract gained `dependsOnByStack` after design.
   Calling the first cut `1.0.0-alpha` matches reality and gives an
   explicit graduation path through `-rc.N` to stable.

4. **Lower review and audit cost for the maintainer.** Four release PRs,
   four CHANGELOG-cycle reviews, four publish gates, four sets of release
   notes. The cost of a single combined alpha review and a single combined
   `RELEASING.md` exercise of the build/verify/deploy gate is materially
   lower for a single-maintainer project.

## Consequences

### Positive

- **One source of truth for the version.** The version-drift mess
  (manifest 0.1.0 / version.txt 0.2.0 / README 1.1.0) is replaced by a
  single `release-please-manifest.json` and a documented graduation path.
- **The first publish exercises the full release pipeline.** Every gate
  (build → verify → deploy) is hit on a real artifact, with the
  consolidated feature surface, before any user depends on us.
- **Documentation consistency is achievable.** A single scope statement
  ("1.0.0-alpha") replaces four separate scope statements that drifted
  out of sync (`ROADMAP.md`, `CHANGELOG.md`, the issue templates'
  "not before v1.2" gates, several ADR references to "v1.2+ will add").

### Negative

- **The CHANGELOG `[Unreleased]` entry is large.** It captures four
  releases' worth of features. We accept this — release-please will
  fold it into the `[1.0.0-alpha]` section on the next release PR.
- **Reviewers can't bisect by release boundary.** Anyone debugging
  behaviour that "regressed in v1.2" has no v1.2 to bisect against.
  Acceptable because there were no users between v1.0 and 1.0.0-alpha.
- **The roadmap's narrative arc is lost.** "v1.1 brought k8s, v1.2 TS,
  v1.3 frontend, v1.4 policy" is a tidier story than "1.0.0-alpha
  bundled all four". Acceptable — the goal is a working tool, not a tidy
  release history.

### Neutral / process

- `ROADMAP.md` retains the original v1.1–v1.4 breakdown under "Folded
  into 1.0.0-alpha", as historical context for anyone reading old issues
  or PRs that reference those versions.
- The original execution plan (formerly `plans/v1.1-backport.md`) lives
  in [`docs/history/v1.0.0-alpha-backport.md`](../history/v1.0.0-alpha-backport.md)
  for traceability. The `plans/` directory is removed since there's no
  longer a pending plan there.

## Alternatives considered

1. **Keep four separate releases as planned.** Rejected: ships partial
   products, multiplies release-cycle cost, and the work is already
   merged on a single branch.

2. **Cut a 1.0.0 stable directly.** Rejected: surface is too broad to
   commit to semver yet. Two consumers in the wild would expose at least
   one design choice that wants revising.

3. **Cut 1.1.0 directly (skip 1.0.0).** Rejected: the version-number
   story already has too much drift; bumping past 1.0 without ever
   shipping a 1.0 amplifies the confusion. `1.0.0-alpha` is the cleanest
   re-anchor.

4. **Keep version 0.x while iterating.** Rejected: the roadmap explicitly
   spent four releases' worth of design effort to reach the surface that
   1.0 was supposed to commit to. Calling it 0.x indefinitely punts on
   the commit and never converges.
