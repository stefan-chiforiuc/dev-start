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

## Two paths to running code

### Fastest — GitHub Codespaces (zero local install)

1. Click **Use this template** on the
   [`dev-start-example`](https://github.com/stefan-chiforiuc/dev-start) repo.
2. Click **Open in Codespaces**.
3. In ~60 seconds: VS Code in the browser, stack running, seeded DB,
   Swagger open, tests green.

### Local

```sh
dotnet tool install -g DevStart
dev-start new my-app
cd my-app
just up         # brings up Postgres, Keycloak, Seq, Jaeger, Mailhog, MinIO
just test       # Testcontainers-backed integration tests
```

Then open the **dashboard at <http://localhost:4000>** for links to every
running service.

---

## What you get in a generated repo

```text
my-app/
  src/
    My.Api/                 # minimal API host + composition root
    My.Domain/              # aggregates, events, value objects
    My.Application/         # CQRS handlers, validators
    My.Infrastructure/      # EF Core, outbox, external clients
  tests/
    My.IntegrationTests/    # Testcontainers + full stack
    My.ArchitectureTests/   # NetArchTest rules
  .claude/                  # CLAUDE.md + skills + agents + MCP config
  .devcontainer/            # Codespaces-ready
  .devstart.json            # capability manifest
  compose.yml, Tiltfile, justfile
  .http/                    # VS Code REST Client requests
  .github/workflows/        # build + test + CodeQL + Trivy + release-please
  docs/adr/
```

Built-in capabilities (composable — add only what you need):

| Capability | What it wires |
|---|---|
| `postgres` | EF Core + Npgsql + migrations + seed + Testcontainers harness |
| `auth`     | OIDC client + in-compose Keycloak realm + sample secured endpoint |
| `otel`     | OpenTelemetry traces/metrics/logs, Jaeger + Seq |
| `queue`    | RabbitMQ + MassTransit + outbox pattern |
| `cache`    | Redis + `IDistributedCache` wrapper |
| `s3`       | MinIO + AWS SDK + signed-URL helper |
| `mail`     | Mailhog + `IEmailSender` |
| `sdk`      | TypeScript SDK generated from OpenAPI |
| `flags`    | OpenFeature provider interface |
| `gateway`  | YARP reverse proxy (for multi-service mode) |

---

## The four CLI verbs

```sh
dev-start new <name>         # scaffold a new project (wizard)
dev-start add <capability>   # add a capability to an existing project
dev-start doctor             # diagnose drift, missing env, broken services
dev-start upgrade            # open a PR with the delta against latest template
```

Same tool on day 0 and day 300.

---

## Opinions

These are intentional defaults. Every one has an ADR explaining *why* and a
[`docs/when-to-leave-the-road.md`](./docs/when-to-leave-the-road.md) entry
explaining how to swap it out.

- **ASP.NET Core minimal APIs** over controllers.
- **EF Core + Npgsql** over Dapper (for migrations + LINQ ergonomics).
- **MediatR + FluentValidation** for CQRS handlers.
- **Serilog (JSON) + OpenTelemetry** — structured from minute 0.
- **xUnit + Testcontainers** — real DB in tests, no mocks for EF.
- **Docker Compose + `just` + Tilt** — not Make.
- **MIT licence**, conventional commits, release-please for releases.
- **Modular monolith first** — multi-service mode exists but is not the default.

---

## Getting involved

This is pre-v1 and opinionated on purpose. Before filing an issue, read the
relevant ADR — most disagreements are covered there, with escape hatches.

- Bugs and feature requests: GitHub Issues.
- Security reports: see [`SECURITY.md`](./SECURITY.md).
- Contributing: [`CONTRIBUTING.md`](./CONTRIBUTING.md).

Licence: MIT.
