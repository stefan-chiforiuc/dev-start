---
name: add-endpoint
description: Scaffold a full vertical slice — aggregate, CQRS command/query handlers, EF migration, minimal-API route, and integration test — following the house paved road.
---

# `/add-endpoint <name>`

Use when the user asks to add a new endpoint, resource, or CRUD surface.

The slice spans every layer. Do them in this order so each step is testable.

## 1. Ask if you need to

If the user only asked for "an endpoint for X", default to a full CRUD slice
(`POST`, `GET /{id}`, `GET`, `PATCH`, `DELETE`). If they want less, confirm.

## 2. Domain

Create in `src/*.Domain/<Name>/`:

- `<Name>.cs` — aggregate root, private setters, factory method, invariants
  enforced in constructor / factory.
- `<Name>Id.cs` — strongly-typed ID (record struct).
- Relevant value objects.
- `<Name>Created.cs`, `<Name>Updated.cs`, `<Name>Deleted.cs` — domain events
  implementing `IDomainEvent`.

Invariants that cannot be expressed via the type system get checks in the
constructor that throw `DomainException`.

## 3. Application

Create in `src/*.Application/<Name>/`:

- `Commands/Create<Name>.cs` — MediatR `IRequest<Result<<Name>Id>>`, handler,
  `FluentValidation.AbstractValidator<Create<Name>>`.
- `Commands/Update<Name>.cs`, `Delete<Name>.cs`.
- `Queries/Get<Name>ById.cs`, `List<Name>.cs`.
- DTOs in `Contracts/`.

Commands **must** load the aggregate via the repository, call its methods,
and persist via `SaveChangesAsync`. No business logic in handlers.

## 4. Infrastructure

- EF configuration class in `src/*.Infrastructure/Persistence/Configurations/<Name>Configuration.cs`.
- Add to `ApplicationDbContext.OnModelCreating`.
- Run `just db-migrate-add Add<Name>`.

## 5. API

In `src/*.Api/Endpoints/<Name>Endpoints.cs`:

- Route group `app.MapGroup("/v1/<lower-plural>").WithTags("<Name>")`.
- One route per command/query; each is a one-liner that sends to MediatR
  and projects the response.
- Attach `.WithOpenApi(o => ...)` with summary + description.

Register the group in `Program.cs` alongside the others.

## 6. Tests

Create `tests/*.IntegrationTests/<Name>Tests.cs`:

- Happy path for every command.
- One invariant violation test per rule in the aggregate.
- One permission test per secured route (`[Authorize]`).

Use the `ApiFactory` fixture and the Testcontainers Postgres lifecycle
already present — don't add a new test harness.

## 7. OpenAPI + `.http`

- Rebuild — `src/*.Api/openapi.json` regenerates. Commit it.
- Add a request recipe to `.http/<name>.http` with token fetch + the
  common calls.

## 8. Verify

- `just test` — green.
- `just lint` — green.
- `dev-start doctor` — green.
- Hit the endpoints from `.http/<name>.http`; confirm a trace appears in
  Jaeger and structured logs in Seq.

## Do not

- Do not add a controller.
- Do not skip FluentValidation.
- Do not add a second aggregate in the same slice — scope creep.
- Do not publish integration events inline — use the outbox (ADR 0005).
