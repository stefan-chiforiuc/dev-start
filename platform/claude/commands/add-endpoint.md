---
description: Scaffold a new minimal-API endpoint group following the Orders template
argument-hint: <ResourceName> (plural, PascalCase)
---

Create a new endpoint group for `$ARGUMENTS` following the exact shape of
the Orders sample:

1. **Domain** — create the aggregate under `src/*.Domain/$ARGUMENTS/`:
   - `$ARGUMENTSId.cs` — strongly-typed id (readonly record struct).
   - `$ARGUMENTS.cs` — aggregate root inheriting `AggregateRoot`, with
     a static factory method that validates invariants and raises an
     initial domain event.
   - Any value objects as `record` types.
2. **Application** — under `src/*.Application/$ARGUMENTS/`:
   - `Contracts/$ARGUMENTSDto.cs` — DTOs exposed by the API.
   - `Commands/` — one file per command (request + validator + handler).
   - `Queries/` — one file per query (request + handler).
   - Handlers take `IAppDbContext` — never `ApplicationDbContext`.
3. **Infrastructure** — EF `IEntityTypeConfiguration<$ARGUMENTS>` under
   `src/*.Infrastructure/Persistence/Configurations/`.
4. **Api** — under `src/*.Api/$ARGUMENTS/`:
   - `$ARGUMENTSEndpoints.cs` with a `Map$ARGUMENTS` extension method.
   - Call it from `MapEndpoints` in `DependencyInjection.cs`.
5. **Tests** — add integration tests in `tests/*.IntegrationTests/$ARGUMENTS/`
   following the shape of `Orders/OrdersTests.cs`.
6. **http** — add a `.http/$ARGUMENTS.http` with at least one request per
   endpoint.
7. **Migration** — invoke `/add-migration Add$ARGUMENTS` and review before applying.

Stick to the house style; deviating from the Orders shape needs an ADR.
