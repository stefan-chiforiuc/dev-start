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
just install-hooks   # pre-commit (gitleaks, dotnet format, markdownlint)

# day-to-day
just build
just test
just lint
```

Multi-service testing uses Tilt:

```sh
cd examples/multi-service
tilt up
```

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
- [ ] Conventional-commit style PR title.

## What not to send

- New stacks (TS, Go, Python) before v1.2 — see the roadmap.
- Kubernetes manifests before v1.1 — see the roadmap.
- Vendor integrations that replace the default opinions — send an escape
  hatch doc instead.
