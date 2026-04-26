# dev-start

An opinionated, fast on-ramp for new .NET projects — plus the tooling to keep
them healthy for the rest of their lives.

`dev-start` is a .NET global tool that scaffolds a production-shaped ASP.NET
Core project with database, auth, observability, CI/CD, security gates,
architecture tests, and a pre-briefed `.claude/` AI assistant already wired.
It also stays useful after day 1 — add capabilities, diagnose drift, and
upgrade templates through the same CLI.

> Status: **pre-v1**. The repo is being built in public; nothing is stable
> yet. See [`ROADMAP.md`](./ROADMAP.md).

---

## Why

Every new project burns the same week on the same plumbing: Dockerfiles,
connection strings, migrations, auth stub, logs, traces, CI, pre-commit,
branching docs. Then the team never cleans it up, never standardises, and
each project drifts.

`dev-start` collapses that week into minutes, commits to one good set of
opinions, and makes the opinions **composable** so you can add a cache, a
queue, or S3 in one command months later.

---

## Quickstart

```sh
dotnet tool install -g DevStart
dev-start new my-app
cd my-app
just up
```

Full quickstart (Codespaces path, local path, troubleshooting, first
real change): [`docs/golden-path.md`](./docs/golden-path.md).

---

## Learn more

- **[`docs/golden-path.md`](./docs/golden-path.md)** — how to run; CLI verbs.
- **[`docs/paved-road.md`](./docs/paved-road.md)** — every default we ship
  and why.
- **[`docs/when-to-leave-the-road.md`](./docs/when-to-leave-the-road.md)** —
  supported ways to deviate.
- **[`capabilities/README.md`](./capabilities/README.md)** — the full
  capability registry and how to author a new one.
- **[`docs/adr/`](./docs/adr/)** — architecture decision records.
- **[`ROADMAP.md`](./ROADMAP.md)** — version targets and scope boundaries.

---

## Getting involved

This is pre-v1 and opinionated on purpose. Before filing an issue, read the
relevant ADR — most disagreements are covered there, with escape hatches.

- Bugs and feature requests: GitHub Issues.
- Security reports: see [`SECURITY.md`](./SECURITY.md).
- Contributing: [`CONTRIBUTING.md`](./CONTRIBUTING.md).
- Releases: [`RELEASING.md`](./RELEASING.md).

Licence: MIT.
