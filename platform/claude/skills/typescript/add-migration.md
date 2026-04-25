---
name: add-migration
description: Create a forward-only SQL migration runnable by the project's migrator.
---

# /add-migration

1. Find the highest-numbered file in `apps/api/migrations/` and add a new
   one: `NNNN_<snake_description>.sql` where NNNN is monotonically
   incremented (zero-padded to 4).
2. Write the migration in plain SQL, idempotent where cheap
   (`CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`).
3. Forward-only. If a change is destructive (drop column, rename), add a
   two-step migration: ship the additive change first, backfill, then
   remove the old column in a later release.
4. For data transforms that can't be expressed in SQL, add a `.ts` shim
   under `apps/api/src/infra/data-migrations/` and call it from the
   migrator — but prefer SQL.
5. Add/update an integration test that exercises the new shape.
