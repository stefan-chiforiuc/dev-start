# When to leave the road

Opinionated defaults exist for a reason, but they're not universal. This
page documents the supported ways to deviate — each with a cost.

> Rule of thumb: if you're leaving the road, **write down why in your
> project's `docs/adr/`**. Future you will thank you.

## Data layer: swap EF Core for Dapper

**When it's reasonable.** Hot paths with complex SQL, OLAP-style queries,
or teams with deep SQL culture.

**How.**

1. `dotnet add package Dapper`.
2. Keep EF for migrations and writes; use Dapper in a sibling
   `Infrastructure/Queries/*` namespace for reads.
3. Update `docs/adr/` with the rationale; update architecture tests so
   the new namespace is allowed to reference `Npgsql` directly.

**What breaks.** `dev-start upgrade` may conflict with your Dapper
queries if we change the EF schema-generation conventions. Treat upgrade
PRs as a review checkpoint, not a rubber-stamp.

## Message bus: swap MassTransit for Dapr / native RabbitMQ

**When it's reasonable.** You're already running Dapr in your platform,
or MassTransit's abstractions fight the broker you need (e.g. Kafka
with strict ordering).

**How.**

1. Remove the `queue` capability entries from `.devstart.json`.
2. Add your chosen package(s); follow their docs for handler registration.
3. Reimplement the outbox with your library of choice, or accept the
   consistency trade-off if you don't.

**What breaks.** The `/add-event` Claude skill assumes MassTransit and
will generate the wrong code. Either rewrite the skill in your
`.claude/skills/` or remove it.

## Auth: swap Keycloak for Auth0 / Azure AD / Clerk

**When it's reasonable.** You already have an identity provider; the
dev experience of Keycloak adds no value when your team knows the other
tool.

**How.**

1. Remove the Keycloak service from `compose.yml` (kept only for the
   local OIDC IdP).
2. Point the API at your IdP's OIDC discovery URL via env.
3. If you're using Auth0/Clerk, add their SDK for richer token claims;
   the `Microsoft.AspNetCore.Authentication.JwtBearer` middleware stays
   as-is.

**What breaks.** Nothing in the generated code — we only use OIDC
primitives.

## Tests: BDD / MSTest / NUnit

**Not supported.** The test harness (Testcontainers extensions, fixture
registration, `WebApplicationFactory` helpers) is xUnit-specific.
Switching means rewriting those helpers. Available effort is better
spent elsewhere — please don't open a PR for this.

## Deploy target: Kubernetes / ECS / Cloud Run

**Kubernetes is on the road.** `dev-start add k8s` ships Helm + Kustomize
overlays plus the `dev-start promote <env>` verb. See the [k8s capability
README](../capabilities/k8s/README.md).

For ECS, Cloud Run, or other container platforms today:

1. The generated `Dockerfile` is platform-agnostic; use it as-is.
2. Copy the env-var contract from `.env.example` into your
   deploy tool's config.
3. Add your platform's smoke-test step to the CI workflow.

## Stack: Node / Go / Python instead of .NET

**TypeScript + Fastify is on the road** as a sibling stack: `dev-start new
--stack typescript` ships the `ts-base` foundation and a parallel set of
`ts-*` capabilities (`ts-postgres`, `ts-auth`, `ts-otel`, etc.) — see
[ADR 0008](./adr/0008-ts-prefix-for-typescript-capabilities.md). The
`frontend` capability is cross-stack.

**Go and Python are not planned.** If you need them, fork the
`capabilities/` model and port what you need. The `.devstart.json` schema
is deliberately language-agnostic.

## The escape hatch of last resort

Delete `.devstart.json`. The tool will no longer manage the project.
Everything the tool generated is plain .NET, plain Docker, plain YAML —
it keeps working. You lose `add`, `doctor`, and `upgrade`; you keep the
code.

We consider this a successful outcome. The goal is that `dev-start` is
useful, not sticky.
