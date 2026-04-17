# postgres capability

Wires EF Core + Npgsql with a production-shaped baseline: migrations,
optimistic concurrency, outbox-ready `DbContext`, `SaveChanges`
interceptor that dispatches domain events, and a Testcontainers
integration-test base.

## Wires

- `Infrastructure/Persistence/ApplicationDbContext.cs` with `xmin` concurrency
  token convention.
- `Infrastructure/Persistence/Configurations/` — one `IEntityTypeConfiguration<T>`
  per aggregate.
- `Infrastructure/Persistence/Interceptors/DomainEventsInterceptor.cs`.
- `Infrastructure/Persistence/Migrations/` with an initial migration.
- Seed data helper — runs on boot in `Development`.
- Tests: `IntegrationTests/Support/ApiFactory.cs` + `PostgresFixture.cs`
  using Testcontainers.

## Opinions

- **xmin** as the concurrency token — no manual rowversion column.
- **Snake case** naming convention (EFCore.NamingConventions).
- **Migrations applied in tests** — never mock EF.

## Escape hatches

- See `docs/when-to-leave-the-road.md#data-layer-swap-ef-core-for-dapper`.
