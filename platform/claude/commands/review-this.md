---
description: Review the current branch against the paved road and ADRs
---

Review the current branch's changes against the project's paved road and
ADRs. Look for:

1. **Layering violations** — anything in Application referencing
   Infrastructure directly (use `IAppDbContext`), anything in Domain
   referencing anything else.
2. **Endpoint style** — all new endpoints should use minimal APIs,
   belong to a MapGroup, ship OpenAPI summaries, and authorize by
   default (opt into `.AllowAnonymous()` explicitly).
3. **Validation** — every command has a FluentValidation validator.
4. **Persistence** — every aggregate has an `IEntityTypeConfiguration`.
   Fluent config over attributes.
5. **Domain events** — aggregates raise events; handlers live under
   `Application/*/EventHandlers/`.
6. **Tests** — each new behaviour has a Domain unit test or an
   integration test.
7. **Observability** — logs use structured Serilog; hot paths have an
   `ActivitySource` span.

Run `just test` first to confirm the branch is green. If it isn't,
report the failure and stop — don't review dead code.

Report findings in priority order (breaking > bad pattern > nitpick).
