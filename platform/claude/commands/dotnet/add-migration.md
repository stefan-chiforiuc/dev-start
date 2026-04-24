---
description: Add a new EF Core migration + review before applying
argument-hint: <MigrationName>
---

Add a new EF Core migration named `$ARGUMENTS` and walk me through the
generated SQL before applying it.

Steps:

1. Run `just db-migrate-add $ARGUMENTS`.
2. Read the generated file under `src/*.Infrastructure/Persistence/Migrations/`.
3. Summarise the schema changes in one paragraph.
4. Flag any risks (destructive drops, non-default column types, missing
   indices, broken FKs).
5. Only after I confirm, run `just db-migrate` to apply it.

If the migration needs a data backfill, propose a separate migration for
that step — never pack schema + backfill into the same migration.
