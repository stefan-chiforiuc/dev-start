# dev-start

An opinionated, fast on-ramp for new .NET projects — plus the tooling to keep
them healthy for the rest of their lives.

`dev-start` is a .NET global tool that scaffolds a production-shaped ASP.NET
Core project with database, auth, observability, CI/CD, security gates,
architecture tests, and a pre-briefed `.claude/` AI assistant already wired.
It also stays useful after day 1 — add capabilities, diagnose drift, and
upgrade templates through the same CLI.

> Status: **1.0.0-alpha (unreleased)**. Pre-release; surface is opinions-locked
> but subject to alpha-cycle adjustment. CLI verbs — `new` / `add` / `doctor` /
> `upgrade --apply` / `promote` / `policy`. Supports both the .NET/ASP.NET
> Core stack (default) and the TypeScript/Fastify stack (`--stack typescript`).
> See [`CHANGELOG.md`](./CHANGELOG.md) for release notes,
> [`RELEASING.md`](./RELEASING.md) for the release pipeline, and
> [`ROADMAP.md`](./ROADMAP.md) for what's next.

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

## Getting started

```sh
dotnet tool install -g DevStart --prerelease   # pre-release while on 1.0.0-alpha
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

Built-in capabilities (composable — add only what you need). Type the
**flat name** in any stack; the CLI resolves it to the right
implementation based on the project's stack (see
[ADR 0010](./docs/adr/0010-flat-capability-names-and-families.md)).

| Capability | What it wires |
|---|---|
| postgres      | EF Core / Kysely + migrations + Orders slice |
| auth          | OIDC via Keycloak |
| otel          | Traces/metrics/logs via OTLP + Jaeger + Seq |
| queue         | RabbitMQ publisher/consumer |
| cache         | Redis wrapper |
| s3            | MinIO + signed-URL helper |
| mail          | Mailhog + mailer |
| flags         | OpenFeature |
| sdk           | TS SDK generated from OpenAPI |
| gateway       | Reverse proxy for multi-service mode |
| k8s           | Helm chart + Kustomize overlays (dev/stage/prod) |
| frontend      | Vite + React 19 + TanStack — consumes the SDK |
| deploy        | Pick a target: `dev-start add deploy --target fly` or `--target aca` |

```sh
dev-start add cache                  # resolves to cache / ts-cache per stack
dev-start add deploy --target fly    # resolves to deploy-fly / ts-deploy-fly
```

The on-disk folders still carry the `ts-` prefix for contributor
clarity; typing them explicitly (`dev-start add ts-auth`) remains an
escape hatch.

---

## The CLI verbs

```sh
dev-start new <name>                                # interactive wizard (stack, backend, extras, deploy)
dev-start new <name> --no-interactive               # use defaults; flags act as overrides
dev-start add <capability>                          # add a capability to an existing project
dev-start add deploy --target fly|aca               # pick a deploy target as a parameter
dev-start doctor [--fix]                            # diagnose drift, missing env, broken services
dev-start upgrade [--apply]                         # refresh the project from latest templates
dev-start list                                      # list capabilities, grouped by family
dev-start promote <env>                             # emit k8s values for dev | stage | prod
dev-start policy list|apply|remove|validate         # org-level policy bundles
dev-start capability new <name>                     # author a new capability from the skeleton
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

This is pre-1.0 and opinionated on purpose. Before filing an issue, read
the relevant ADR — most disagreements are covered there, with escape
hatches.

- How to ask for help: [`SUPPORT.md`](./SUPPORT.md).
- Contributing: [`CONTRIBUTING.md`](./CONTRIBUTING.md).
- Architecture (CLI internals): [`ARCHITECTURE.md`](./ARCHITECTURE.md).
- Release pipeline: [`RELEASING.md`](./RELEASING.md).
- Security reports: [`SECURITY.md`](./SECURITY.md).

Licence: MIT.
