# CLAUDE.md — dev-start

Project memory for Claude. Read once per session.

## What this repo is

`dev-start` is a .NET global tool that scaffolds opinionated ASP.NET Core
(and TypeScript/Fastify) projects and stays useful afterward via `add`,
`doctor`, `upgrade`, `promote`, `policy`. The CLI lives in
`src/DevStart.Cli/`. Capability modules in `capabilities/` are what
actually get composed into a generated project. See `ARCHITECTURE.md` and
`docs/adr/` for the model.

## Local dev loop

```sh
just build
just test                # 199 tests; fast, deterministic, no external services
just sandbox             # end-to-end matrix: builds CLI + scaffolds + dotnet builds each
just sandbox-new <Name>  # one-off scaffold into .sandbox/
just lint                # dotnet format --verify-no-changes
```

`just sandbox` is the gate that catches what `just test` can't: broken
`.sln`, malformed templates, capabilities that scaffold but don't compile.
**Always run it** before pushing if you touched `Tokens`, `Planner`,
`CapabilityInstaller`, `_shared/`, or anything under `capabilities/`.

## Bug catalog — read before touching adjacent code

`docs/bug-catalog.md` lists every fixed bug with symptom → root cause →
regression guard. Re-read the relevant entry before changing those
surfaces; append a new `BUG-NNN` entry whenever you fix one.

## Capability completeness

A capability is mergeable only when it satisfies
[ADR 0011](docs/adr/0011-capability-definition-of-done.md): manifest,
README, files, injectors, wizard wiring, at least one Variations test
row, doctor checks, CHANGELOG entry. Most enforced by integrity tests.

## PR workflow (durable — do not skip)

1. **Auto-open a PR on the first push.** When you push commits to a
   non-main branch in this repo and there is no open PR for that branch,
   call `mcp__github__create_pull_request` immediately. Don't wait for
   the user to ask.
2. **Keep the PR description in sync.** After every subsequent code
   change that lands on the branch (commit + push), update the PR
   description with `mcp__github__update_pull_request` so it reflects
   the current state of the work — current summary, current test plan,
   any new files/decisions worth flagging. Don't let the description go
   stale.
3. **Body shape.** Use the template the existing PRs follow: a short
   `## Summary` (bullets), then `## Test plan` (checkbox list of what
   was actually verified, marked `[x]` when done), then the
   `https://claude.ai/code/session_...` footer link.
4. **PR title** stays a single conventional-commit line. Update it if
   the scope changes meaningfully across follow-up commits.
5. **Don't merge** unless the user explicitly asks. Don't post extra
   comments on the PR for routine updates — edit the body instead.

## Things to be careful with

- **Releases** are driven by `release-please` from conventional-commit
  messages. Don't bump versions by hand. The `[Unreleased]` block in
  `CHANGELOG.md` is hand-editable; everything below it is generated.
- **Action pinning policy** (`.github/workflows/`): third-party actions
  must be pinned to a 40-char SHA. The `Verify action SHA pins` CI job
  enforces this.
- **Scaffolded `global.json`** must allow any installed SDK in the major
  version (use a `.x00` patch + `rollForward: latestFeature`). Pinning a
  specific patch breaks `dotnet sln add` for anyone without that exact
  SDK — see BUG-003.
