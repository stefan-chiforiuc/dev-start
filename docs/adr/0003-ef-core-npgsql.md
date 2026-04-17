# 3. EF Core + Npgsql as the default data stack

Date: 2026-04-17
Status: Accepted

## Context

We need a default ORM/DB story. The candidates are EF Core + Npgsql,
Dapper, and a pairing of both. We also need to pick a database; Postgres
is the pragmatic choice for almost every use case, and explicitly
rejecting SQL Server means we don't need to generalise.

## Decision

- **Postgres 16** as the default database.
- **EF Core + Npgsql** as the default ORM and migration engine.
- **EF migrations** applied by the test harness and by a one-shot migration
  job in CI/deploy.
- Dapper **allowed** in `Infrastructure/Queries/*` for hot read paths —
  see `docs/when-to-leave-the-road.md`.

## Consequences

- LINQ queries, change tracking, and migrations all come in one package.
- Testcontainers provides a real Postgres in tests; we do not mock EF.
- Cold-start is a few hundred ms longer than a Dapper-only setup; we
  accept this in exchange for migration tooling and type safety.
- EF's outbox support pairs well with MassTransit (see ADR 0005).
- For reporting/analytics workloads, EF is a poor fit; we document the
  Dapper escape hatch rather than trying to serve both.

## Alternatives considered

- **Dapper-only**: faster, but we'd have to pick a separate migration
  tool (DbUp / Flyway / Liquibase) and a separate query-object pattern.
  More moving parts.
- **SQL Server default**: Azure-friendly but excludes a lot of the
  open-source audience; Postgres is fine on Azure too.
- **NoSQL default (Mongo/DynamoDB)**: not a good fit for most business
  apps; we'd rather deliberately exclude and document than half-commit.
