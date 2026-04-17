# 2. Use ASP.NET Core minimal APIs (not controllers)

Date: 2026-04-17
Status: Accepted

## Context

ASP.NET Core offers two HTTP-surface models: MVC controllers and minimal
APIs. Both are supported long-term by the .NET team.

We are scaffolding new projects from scratch — none of them have legacy
controllers to live alongside.

## Decision

Generated projects use **minimal APIs** exclusively, organised by route
groups per bounded context (e.g. `app.MapGroup("/orders").MapOrders()`).

Handlers are **MediatR** commands/queries, not inlined lambdas. The
endpoint body is a one-liner that sends the request and projects the
response.

## Consequences

- Less ceremony; endpoint files are ~30 LOC instead of ~120.
- OpenAPI generation (Swashbuckle) still works; we annotate route groups
  with `.WithOpenApi(...)`.
- Unit-testing endpoint "shape" is cheaper — we assert over handler
  behaviour, not controller wiring.
- Teams arriving from MVC need a one-page primer. Included in
  `docs/paved-road.md`.
- Some MVC-era extensions (complex model binders, action filters) don't
  transfer; we accept this.

## Alternatives considered

- **Controllers**: more familiar to .NET teams, richer ecosystem for
  filters and model binding. Rejected because the ceremony doesn't pay
  off in the scaffold.
- **FastEndpoints**: strong library, but third-party. We'd rather build
  on the platform default.
