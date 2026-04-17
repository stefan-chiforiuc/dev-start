---
name: add-event
description: Add a domain or integration event with the right wiring and outbox guarantees.
---

# `/add-event <name>`

First decide **which kind**:

- **Domain event** — consumed inside the same service. Raised from an
  aggregate, dispatched after `SaveChanges` by the interceptor. No broker.
- **Integration event** — consumed by other services. Goes through the
  outbox (MassTransit EF outbox). Never raise directly from the domain.

## Domain event

1. `src/*.Domain/<Context>/<Name>.cs` — record implementing `IDomainEvent`.
2. In the aggregate method that triggers it, call `RaiseDomainEvent(new <Name>(...))`.
3. Handler in `src/*.Application/<Context>/EventHandlers/<Name>Handler.cs`
   implementing `INotificationHandler<<Name>>`.
4. Test the handler in `tests/*.UnitTests`.

## Integration event

1. Define the contract in a shared `Contracts` project or folder. Contracts
   are **stable** — version them rather than changing them.
2. In the command handler, after the aggregate work, call
   `outbox.Publish(new <Name>(...))` (a thin wrapper over the MassTransit
   outbox).
3. Add a consumer if this service also consumes it:
   `src/*.Infrastructure/Messaging/Consumers/<Name>Consumer.cs`.
4. Integration test the full path (Testcontainers Postgres + Testcontainers
   RabbitMQ) in `tests/*.IntegrationTests`.

## Rules

- Event payloads carry IDs and minimal state; consumers fetch if they
  need more.
- Never include PII in event payloads unless the consumer has a business
  need; even then, prefer an ID reference.
- Events are immutable. To change the shape, publish a new version.
- Don't fan out: keep each event's fan-out < 5 consumers at publish time.
  More than that usually means you're missing an aggregate.
