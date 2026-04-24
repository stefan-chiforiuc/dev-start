---
description: Add a new forward-only SQL migration + review before applying
argument-hint: <description-in-kebab-case>
---

Add a new SQL migration named `$ARGUMENTS` and walk me through it before
applying.

Steps:

1. Find the highest-numbered file in `apps/api/migrations/` and create
   `NNNN_$ARGUMENTS.sql` (monotonically incremented, zero-padded to 4).
2. Write plain SQL, idempotent where cheap (`CREATE TABLE IF NOT EXISTS`,
   `ADD COLUMN IF NOT EXISTS`). Forward-only.
3. Summarise the schema changes in one paragraph.
4. Flag risks: destructive drops, missing indices, broken FKs, anything
   that would block a rolling deploy.
5. Update `Database` in `apps/api/src/infra/db.ts` to reflect the new shape.
6. Only after I confirm, run `pnpm --filter api migrate` to apply.

If the migration needs a data backfill, propose a separate migration for
that step — never pack schema + backfill into the same migration.
