# queue capability

Reliable async messaging via RabbitMQ + MassTransit + the EF outbox.

## Wires

- RabbitMQ service in compose.
- MassTransit configured with:
  - EF outbox (tables `InboxStates`, `OutboxMessages`, `OutboxStates`).
  - Retry: exponential, 5 attempts.
  - Redelivery: linear, 1/5/15 min then dead-letter.
- `IEventPublisher` — thin wrapper over MassTransit for domain code.
- Sample consumer: `UserRegisteredConsumer` bound to a sample event.
- Integration test: publishes event, asserts consumer effect via
  Testcontainers RabbitMQ + Postgres.

## Opinions

- **Outbox always on.** There's no "just publish" path. See ADR 0005.
- **At-least-once + idempotent consumers** beats promising exactly-once.
- **Dead-letter = manual reprocess.** No auto-replay from DLQ; humans
  decide.

## Escape hatches

- Dapr pub/sub — see `docs/when-to-leave-the-road.md`.
- Kafka — possible with MassTransit's Kafka integration; the outbox
  semantics still apply.
