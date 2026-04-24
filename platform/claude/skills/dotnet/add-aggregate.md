---
name: add-aggregate
description: Add a DDD aggregate with strongly-typed ID, value objects, invariants, and domain events.
---

# `/add-aggregate <name>`

Domain-first. No Application/Infrastructure code until the aggregate is
shaped.

## Structure

`src/*.Domain/<Name>/`:

- `<Name>.cs` — aggregate root.
- `<Name>Id.cs` — `public readonly record struct <Name>Id(Guid Value)`.
- Value objects (`Money`, `Address`, `EmailAddress`, etc.) in the same
  folder.
- `<Name>Created.cs`, `<Name>Updated.cs`, etc. — `IDomainEvent` records.

## Rules

- All setters private. State changes through methods named after the
  business verb (`Cancel`, `MarkShipped`), not `SetStatus`.
- Factory method `public static <Name> Create(...)` that:
  - Validates invariants — throw `DomainException` if violated.
  - Raises `<Name>Created`.
- Never reference `Infrastructure` or `Application`.
- Never take dependencies on EF types (`DbSet`, `DbContext`).
- Rowversion property for optimistic concurrency: `public uint Version { get; private set; }`.

## Events

Domain events are raised via `RaiseDomainEvent(...)` on the aggregate base.
They're dispatched after `SaveChangesAsync` by the `DomainEventsInterceptor`.

If the event needs to cross service boundaries, map it to an integration
message in `Application/<Name>/IntegrationEvents/` and publish via the
outbox — never raise integration events from the domain directly (ADR 0005).

## Tests

`tests/*.DomainTests/<Name>Tests.cs`:

- One test per invariant.
- One test per method that changes state.
- One test confirming the right domain event is raised.

No DB, no fixtures — pure domain tests should run in < 100 ms.
