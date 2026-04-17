# 5. CQRS with MediatR + outbox pattern for messaging

Date: 2026-04-17
Status: Accepted

## Context

Business apps quickly accumulate handlers for commands, queries, and
events. Without a pattern, these live in controllers or service classes
that drift in shape.

Event publishing introduces a consistency trap: if we `INSERT` then
publish, a broker outage loses events; if we publish then `INSERT`, a
DB failure duplicates events.

## Decision

- **MediatR** for command and query dispatch.
- **FluentValidation** as a MediatR pipeline behaviour; failures map to
  `ProblemDetails 422`.
- **Outbox pattern** via MassTransit's EF outbox: domain events and
  integration messages go into an `outbox_messages` table inside the
  same transaction as the aggregate, then a background dispatcher ships
  them to the broker.
- Handlers live in `My.Application/<bounded-context>/Commands` and
  `...Queries`. Event handlers in `...Events`.

## Consequences

- One consistent shape for application-layer work.
- Exactly-once delivery semantics (at-least-once at the broker, dedupe
  on consume).
- Extra moving part: the outbox dispatcher — we run it in-process for
  dev, as a background service.
- Teams that don't need messaging can still use MediatR and skip the
  outbox; the `queue` capability adds both at once when they do.

## Alternatives considered

- **Direct broker publish**: simpler, but consistency trap as above.
- **Event sourcing**: a much bigger commitment; wrong default for a
  scaffold aimed at CRUD-shaped apps.
- **Rebus / NServiceBus**: fine libraries; MassTransit has the
  strongest EF outbox story and widest adoption.
