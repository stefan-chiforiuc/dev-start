---
name: add-migration
description: Add a forward-only EF Core migration that is safe under zero-downtime deploys.
---

# `/add-migration <description>`

Forward-only, blue/green-safe migrations. Always assume the old code is
still running during the deploy.

## Rules

1. **Additive first.** New column → nullable or with a default; backfill in
   a second migration; make NOT NULL in a third once all writers are on the
   new code.
2. **Never rename in place.** Rename = add new, backfill, cut writers over,
   drop old. Each as its own migration.
3. **Never drop in a PR that also adds.** Drops land after the feature that
   stopped using the old column is deployed.
4. **Large backfills are not EF migrations.** Put them in a one-shot job
   under `ops/backfills/`; the migration only changes schema.
5. **No data in `Up()`** beyond defaults for a few rows. If you need more,
   see rule 4.

## Procedure

1. `just db-migrate-add <VerbNoun>` — generate scaffolding.
2. Edit `Up()` and confirm `Down()` is a true inverse (even though we don't
   run it in prod).
3. Update the relevant `*Configuration.cs` in `Infrastructure/Persistence/Configurations`.
4. Regenerate `Database.sql` snapshot: `just db-snapshot`.
5. Run `just test` — Testcontainers applies the migration from scratch
   and against a seeded DB.
6. Verify the migration is idempotent-ish: delete `migrations_history`
   row, re-run, confirm no errors.

## Commit

- One commit per migration.
- Commit message: `feat(db): <description> (#ticket)`.
- If the migration is additive and a second migration will follow, link it
  in the PR description.
