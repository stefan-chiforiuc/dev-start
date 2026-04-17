# Capabilities

The unit of composition in `dev-start`. See [ADR 0006](../docs/adr/0006-capabilities-not-templates.md).

Each capability is a folder that describes:

- **what** it adds to a project,
- **which files** to copy,
- **which patches** to apply to existing files,
- **what other capabilities** it requires,
- **how to verify** it's healthy after install.

## Anatomy

```
capabilities/<name>/
  capability.json      # metadata — see schema below
  README.md            # human docs: what, why, opinions, escape hatches
  files/               # verbatim files copied into the target project
  patches/             # structured edits applied to existing target files
  tests/               # capability-level smoke tests (run in dev-start CI)
```

## `capability.json` schema

```jsonc
{
  "$schema": "https://dev-start.dev/schemas/capability/1.json",
  "name": "postgres",
  "version": "1.0.0",
  "description": "EF Core + Npgsql + migrations + Testcontainers harness",
  "dependsOn": ["base"],
  "conflictsWith": [],
  "addsServices": ["postgres"],   // compose services enabled
  "envAdditions": [
    { "key": "ConnectionStrings__Default", "example": "Host=localhost;Database=app;Username=dev;Password=dev" }
  ],
  "postInstall": [
    "dotnet restore",
    "dotnet ef database update --project src/*.Infrastructure --startup-project src/*.Api"
  ],
  "doctor": [
    { "check": "service", "name": "postgres", "port": 5432 },
    { "check": "env", "name": "ConnectionStrings__Default" }
  ]
}
```

## v1 capabilities

| Capability | Depends on | What it wires |
|---|---|---|
| `base` | — | Minimal .NET 8 API + layered projects + Program.cs composition root |
| `postgres` | `base` | EF Core + Npgsql + migrations + Testcontainers base + seed |
| `auth` | `base` | OIDC + Keycloak realm + sample secured endpoint |
| `otel` | `base` | OpenTelemetry traces/metrics/logs + exporters |
| `queue` | `postgres` | RabbitMQ + MassTransit + EF outbox |
| `cache` | `base` | Redis + `IDistributedCache` wrapper |
| `s3` | `base` | MinIO + AWS SDK + signed-URL helper |
| `mail` | `base` | Mailhog + `IEmailSender` abstraction |
| `sdk` | `base` | OpenAPI + NSwag TS SDK generation |
| `flags` | `base` | OpenFeature provider interface + in-memory impl |
| `gateway` | `base`, `auth` | YARP reverse proxy for multi-service mode |

## Writing a new capability

1. Copy the skeleton: `cp -r capabilities/_skeleton capabilities/<name>`.
2. Fill in `capability.json` and `README.md`.
3. Add files under `files/`.
4. If you need to edit an existing project file, use `patches/` — plain
   `.patch` files applied with `git apply`. Keep them **idempotent**:
   re-running `dev-start add <name>` must succeed even if already applied.
5. Add at least one test under `tests/`.
6. Submit a PR with an ADR explaining the opinion if any.
