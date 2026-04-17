---
name: extract-service
description: Split a bounded context out of the monolith into its own service, using the same capability modules.
---

# `/extract-service <context>`

Use this when a context in the monolith has reached the threshold where
it needs independent deploy, scale, or data ownership. Not before.

## Heuristics for "do we need to?"

- The context has ≥ 2 people owning it full-time.
- Its release cadence is diverging from the rest of the monolith.
- Its scale profile differs materially (e.g. 10x read rate).
- Its data has a consistency boundary that EF transactions are fighting.

If none of these apply, **don't extract**. Modular boundaries inside the
monolith get you 80% of the benefit with none of the distributed-systems
tax.

## Procedure

1. Confirm the context's folder is self-contained: it should already be
   under `src/*.Domain/<Context>/`, `...Application/<Context>/`, etc.
   If references leak across contexts, fix those first **inside the
   monolith** before extracting.
2. Run `dev-start add gateway` on the monolith if not already present.
3. Use `dev-start new <context-service> --from-monolith=<this-repo>` to
   scaffold a new service that reuses the same capability config.
4. Move the context's code across, preserving namespaces as
   `<ServiceName>.Domain` / `.Application` / `.Infrastructure`.
5. Move the DB tables:
   - Prefer separate database, not separate schema.
   - Data migration: dual-write from monolith for one release, then cut
     reads over, then remove writes from monolith.
6. Replace direct in-process calls with either:
   - Integration events through the outbox (preferred).
   - Synchronous HTTP via the gateway (for query-only paths).
7. Update the gateway's YARP config to route `/<context>/*` to the new
   service.

## Pitfalls

- **Distributed transactions.** If you find yourself wanting one, the
  extraction is wrong — redesign around eventual consistency or keep the
  context in the monolith.
- **Chatty calls.** An extracted service that gets called in a loop from
  the monolith is a performance disaster. Batch, or don't extract.
- **Shared DB.** Don't. Two services writing the same rows is an
  anti-pattern; you've added complexity without decoupling.
