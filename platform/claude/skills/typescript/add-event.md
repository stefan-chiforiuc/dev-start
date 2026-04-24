---
name: add-event
description: Scaffold an event publisher + consumer pair.
---

# /add-event

1. Add the event schema in `apps/api/src/events/<name>.ts` — zod schema
   for payload shape + exported TS type.
2. Publisher: add a function in the owning domain module that calls
   `queue.publish('<topic>', payload)` after the DB write in the same
   transaction (or outbox — see ADR 0005 equivalent).
3. Consumer: add `apps/api/src/events/consumers/<name>.ts` with a
   handler; register it at `// devstart:app-consumers` in `app.ts`.
4. Integration test that publishes + asserts side effects.

Never log the full payload if it contains PII; redact via pino.
