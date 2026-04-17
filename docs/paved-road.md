# The paved road

One page. Everything `dev-start` commits to by default.

## Runtime

- **.NET 8 LTS**, nullable reference types on, warnings-as-errors on.
- **ASP.NET Core minimal APIs** with route groups — no controllers.
- **Kestrel** only; no IIS hosting.

## Project layout (clean-ish, not dogmatic)

```
src/
  My.Api/                 # minimal API host, composition root, OpenAPI
  My.Application/         # MediatR handlers, validators, DTOs
  My.Domain/              # aggregates, value objects, domain events
  My.Infrastructure/      # EF Core, outbox, external clients
tests/
  My.IntegrationTests/    # Testcontainers-backed, asserts behaviour
  My.ArchitectureTests/   # NetArchTest rules
```

Dependencies flow inward only: `Api → Application → Domain`;
`Infrastructure → Application → Domain`. Enforced by NetArchTest.

## Data

- **EF Core + Npgsql** (Postgres 16).
- Migrations live in `Infrastructure/Persistence/Migrations`, applied by
  the test harness and by a one-shot `dotnet ef database update` job in
  CI and deploy.
- **Optimistic concurrency** via a `xmin`/`rowversion` column on every
  aggregate.
- **Outbox pattern** (MassTransit EF outbox) for reliable event publish.

## Application patterns

- **MediatR** for commands and queries. No repositories except where a
  domain operation genuinely spans aggregates.
- **FluentValidation** for input validation, mapped to `ProblemDetails`
  `422` responses.
- **Domain events** emitted from aggregates, dispatched after save by a
  `SaveChanges` interceptor, relayed through the outbox.

## API surface

- **`ProblemDetails`** for every error response. A consistent error
  taxonomy lives in `My.Api/Errors`.
- **OpenAPI** generated at build (Swashbuckle), checked into the repo,
  diffed in CI to catch breaking changes.
- **Scalar** (modern API reference UI) at `/docs`.
- **Versioning** via URL segment (`/v1/...`). Adding v2 requires an ADR.

## Observability

- **Serilog → OpenTelemetry** for structured logs (JSON to stdout).
- **OTel traces + metrics** via OTLP to Jaeger (dev) / your collector
  (prod).
- **`/healthz`** (liveness) and **`/readyz`** (readiness) wired to the DB
  and any downstream dependencies.

## Auth

- **OIDC** via Keycloak locally, any OIDC IdP in prod.
- **`[Authorize]`** by default on every endpoint; opt-out with
  `[AllowAnonymous]`.
- Short-lived access tokens, refresh in the client; the API never stores
  user passwords.

## Tests

- **xUnit** + **FluentAssertions** + **Testcontainers** (Postgres,
  RabbitMQ, etc.).
- Integration tests spin up the real dependencies; we don't mock EF.
- Architecture tests enforce layering and naming conventions.

## Dev loop

- **`just`** as the task runner (works on Windows without WSL).
- **Docker Compose** for the backing stack (Postgres, Keycloak, Seq,
  Jaeger, Mailhog, MinIO, RabbitMQ when enabled).
- **Tilt** for multi-service mode (hot reload + unified UI).
- **`.http` files** for request recipes, VS Code REST Client compatible.

## CI / CD

- **GitHub Actions**, reusable workflow at `.github/workflows/dotnet-ci.yml`.
- **Conventional commits** + **release-please** for tags and changelogs.
- **CodeQL**, **Trivy**, **gitleaks** gates on every PR.
- **API diff** gate on the checked-in OpenAPI spec.
- **k6 smoke** with a p95 budget on the sample endpoint.
- Containers signed with **cosign**; SBOM via **CycloneDX**; SLSA L2
  provenance via `actions/attest-build-provenance`.

## Deploy

- **Fly.io** or **Azure Container Apps** recipes for v1 (pick at
  generate-time). Kubernetes from v1.1.
- 12-factor config from env vars only; no appsettings secrets.

## AI

- **`.claude/` bundle** in every generated repo — CLAUDE.md briefed on
  the ADRs, skills for common edits, agents for review and architecture,
  MCP config pointing at the live compose stack.

## What's not on the road

- Non-Postgres RDBMS, NoSQL, event stores as first-class options.
- GraphQL or gRPC as the primary surface.
- DI containers other than the built-in.
- Test frameworks other than xUnit.

If you need one of these, see `docs/when-to-leave-the-road.md`.
