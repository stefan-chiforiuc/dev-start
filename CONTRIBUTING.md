# Contributing to dev-start

Thanks for considering a contribution. This project is **pre-v1** and
opinionated on purpose — before opening an issue or PR, please read this
document and the relevant ADR(s) in [`docs/adr/`](./docs/adr).

## Ground rules

1. **Start with an issue.** For anything bigger than a typo, open an issue
   first so we can agree on the shape before you invest time.
2. **Opinions have ADRs.** If you want to change a default (ORM, logging,
   test framework, etc.), write an ADR-style proposal in the issue. We
   don't litigate opinions in PR threads.
3. **Escape hatches over forks.** If a default doesn't fit your team, we'd
   rather document the escape hatch in
   [`docs/when-to-leave-the-road.md`](./docs/when-to-leave-the-road.md)
   than add a second opinion.
4. **Capabilities, not templates.** New features generally land as a
   capability module under `capabilities/<name>/` that can be added to an
   existing project, not as a new monolithic template.

## Development loop

```sh
# one-time
dotnet tool restore
just install-hooks   # copies platform/hooks/pre-commit → .git/hooks/pre-commit

# day-to-day
just build
just test
just lint
just sandbox         # end-to-end: scaffold a matrix into .sandbox/ and dotnet build each
```

`just sandbox` is the gate that catches the class of bug `just test` can't:
broken `.sln`, malformed templates, capabilities that scaffold but don't
compile. It's fast — under a minute on a warm cache — and runs the **real**
CLI binary against fresh projects in `.sandbox/`. Any time you touch
`Tokens`, `Planner`, `CapabilityInstaller`, the `_shared/` overlays, or
anything under `capabilities/`, run it before pushing.

For one-off reproductions:

```sh
just sandbox-new My.Cool.App --with postgres auth otel
just sandbox-clean
```

The pre-commit hook runs `gitleaks` (secret scan, staged content only),
`dotnet format --verify-no-changes`, and `markdownlint`. Each step is
optional — missing tools are skipped with a warning so contributors aren't
blocked by tooling drift. The hook source is checked in at
[`platform/hooks/pre-commit`](./platform/hooks/pre-commit); edit there and
re-run `just install-hooks`, never edit the installed copy in `.git/hooks/`.

### Capability "Definition of Done"

A capability is reviewable for merge only when it satisfies the checklist in
[ADR 0011](./docs/adr/0011-capability-definition-of-done.md): manifest,
README with escape hatch, files + injectors, wizard wiring, at least one
`Variations` test row, at least one `doctor` check, CHANGELOG entry. Most
of these are enforced statically by `CapabilityIntegrityTests` /
`WizardWiringTests` / `CapabilityCoverageTests` — your PR will fail CI if
they aren't met.

### Bug catalog — don't re-introduce these

Before merging changes that touch the surfaces listed in
[`docs/bug-catalog.md`](./docs/bug-catalog.md), re-read the relevant entry.
Each entry includes the regression guard so you know which test you'd
break if the same shape of bug came back. Add a new `BUG-NNN` entry when
you fix one.

## Commits and releases

- **Conventional commits.** `feat:`, `fix:`, `docs:`, `refactor:`, `test:`,
  `chore:`, `ci:`. Scope optional (e.g. `feat(postgres): add row-level
  security helper`).
- We use **release-please** to produce tagged releases and changelogs from
  commit messages. Don't bump versions by hand.
- **Breaking changes** require a `BREAKING CHANGE:` footer and an ADR
  explaining the migration.

## PR checklist

- [ ] Linked to an issue.
- [ ] Tests added or updated (unit + integration as applicable).
- [ ] Architecture tests still green.
- [ ] If a default changed: ADR added under `docs/adr/`.
- [ ] If a user-visible behaviour changed: entry in the relevant capability's
      `README.md` + `when-to-leave-the-road.md` if it narrows an escape hatch.
- [ ] `just sandbox` is green (mandatory if you touched `Tokens`, `Planner`,
      `CapabilityInstaller`, `_shared/`, or anything under `capabilities/`).
- [ ] If you added a capability: meets ADR-0011 Definition of Done.
- [ ] If you fixed a bug: appended a `BUG-NNN` entry to `docs/bug-catalog.md`
      and the regression guard is in the same PR.
- [ ] Conventional-commit style PR title.

## What not to send

- New stacks (Go, Python, JVM) — the bar is high; open a discussion first.
  TypeScript and .NET are in scope today; see [ADR
  0008](./docs/adr/0008-ts-prefix-for-typescript-capabilities.md).
- Vendor integrations that replace the default opinions — send an escape
  hatch doc instead (see
  [`docs/when-to-leave-the-road.md`](./docs/when-to-leave-the-road.md)).
