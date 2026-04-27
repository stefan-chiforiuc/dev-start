# Capabilities

The unit of composition in `dev-start`. See
[ADR 0006](../docs/adr/0006-capabilities-not-templates.md) for why
capabilities are opinions-sized, and
[ADR 0007](../docs/adr/0007-injectors-over-fork-templates.md) for how
they extend shared files without forking.

Each capability is a folder that describes:

- **what** it adds to a project,
- **which files** to copy verbatim,
- **which injectors** to apply to existing target files,
- **what other capabilities** it requires (and conflicts with),
- **how to verify** it's healthy after install.

## Anatomy

```text
capabilities/<name>/
  capability.json      # metadata (see schema below)
  README.md            # human docs: what, why, opinions, escape hatches
  files/               # verbatim files copied into the target project
                       # (path segments + file contents get token-substituted)
  injectors.json       # spec for structured edits into existing files
  injectors/*.fragment # text fragments referenced by injectors.json
```

`capability.json` carries metadata; `files/` is the tree; injectors extend
base files by inserting fragments at named markers or literal anchors.

## `capability.json` schema

```jsonc
{
  "$schema": "https://dev-start.dev/schemas/capability/1.json",
  "name": "postgres",
  "version": "1.0.0",
  "description": "EF Core + Npgsql + migrations + Testcontainers harness",
  "dependsOn": ["base"],
  "conflictsWith": [],
  "addsServices": ["postgres"],   // compose services enabled by this capability
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

## `injectors.json` schema

```jsonc
{
  "injectors": [
    {
      // Path (tokens resolved) to the target file in the generated project.
      "file": "src/{{Name}}.Infrastructure/DependencyInjection.cs",

      // Either a marker comment (preferred) or a literal anchor. One is required.
      "marker": "// devstart:infrastructure-services",

      // before | after | replace. Default: "after".
      "placement": "after",

      // Path to a file under capabilities/<name>/injectors/ whose contents
      // get inserted. Tokens in the fragment are substituted too.
      "fragment": "infrastructure-services.fragment"
    }
  ]
}
```

Alternative to `marker`: a literal `anchor` string (e.g. `"</Project>"` or
`"\"AllowedHosts\": \"*\""`). Use `marker` when you own the base file and
can drop a named comment; use `anchor` when you're injecting into a
widely-known fixed point.

Injectors are **idempotent**: if the trimmed fragment body is already
present in the target file, the injector skips. Rerunning
`dev-start add <cap>` on a live project is safe.

## Capabilities

Auto-included by the foundation: `base` (.NET) and `ts-base` (TypeScript).
Both are selected by `dev-start new` based on `--stack` and are not
user-installable via `add`.

### .NET stack

| Capability | Depends on | What it wires |
|---|---|---|
| `postgres` | `base` | EF Core + Npgsql + migrations + Testcontainers base + seed + sample Orders slice |
| `auth` | `base` | OIDC + in-compose Keycloak realm + `/me` endpoint |
| `otel` | `base` | OpenTelemetry traces/metrics/logs + OTLP exporter |
| `queue` | `postgres` | RabbitMQ + MassTransit + EF outbox + integration-event bridge sample |
| `cache` | `base` | Redis + `IDistributedCache` + `ITypedCache` abstraction |
| `s3` | `base` | MinIO + AWS SDK + signed-URL helper |
| `mail` | `base` | Mailhog + `IEmailSender` abstraction |
| `sdk` | `base` | OpenAPI + `openapi-typescript` + `openapi-fetch` TS client |
| `flags` | `base` | OpenFeature provider + in-memory dev impl |
| `gateway` | `base` | YARP reverse proxy for multi-service mode |
| `deploy-fly` | `base` | `fly.toml` + Fly.io deploy workflow (auto-included with `--deploy fly`) |
| `deploy-aca` | `base` | Bicep + `azure.yaml` + ACA deploy workflow (auto-included with `--deploy aca`) |
| `k8s` | `base` | Helm chart + Kustomize overlays (`dev`, `stage`, `prod`); migration Job gated by `postgres`; ServiceMonitor gated by `otel` |

### TypeScript stack

See [ADR 0008](../docs/adr/0008-ts-prefix-for-typescript-capabilities.md)
for the `ts-` prefix convention and stack gating.

| Capability | Depends on | What it wires |
|---|---|---|
| `ts-postgres` | `ts-base` | Kysely + node-postgres + migrations + Testcontainers + sample Orders slice |
| `ts-auth` | `ts-base` | OIDC via Keycloak; `@fastify/jwt` with `get-jwks` JWKS; `/me` route |
| `ts-otel` | `ts-base` | OpenTelemetry traces/metrics/logs + OTLP exporter |
| `ts-queue` | `ts-postgres` | RabbitMQ + amqplib + outbox |
| `ts-cache` | `ts-base` | Redis + typed cache helper |
| `ts-s3` | `ts-base` | MinIO + AWS SDK v3 + signed-URL helper |
| `ts-mail` | `ts-base` | Mailhog + nodemailer wrapper |
| `ts-flags` | `ts-base` | OpenFeature JS SDK + in-memory dev provider |
| `ts-sdk` | `ts-base` | Auto-generated TypeScript SDK from the API's OpenAPI |
| `ts-gateway` | `ts-base` | Fastify proxy for multi-service mode |
| `ts-deploy-fly` | `ts-base` | `fly.toml` + Fly.io deploy workflow |
| `ts-deploy-aca` | `ts-base` | Bicep + `azure.yaml` + ACA deploy workflow |

### Cross-stack

| Capability | Depends on (.NET) | Depends on (TypeScript) | What it wires |
|---|---|---|---|
| `frontend` | `sdk` | `ts-sdk` | Vite + React 19 + TanStack Router + TanStack Query; `web` service in compose; consumes the generated SDK |

## Writing a new capability

1. Seed it: `dev-start capability new <name>` (from a dev-start checkout).
2. Fill in `capability.json` and `README.md`.
3. Add files under `files/`. Path segments and file contents both get
   `{{Name}}` / `{{name}}` token substitution at install time.
4. If you need to extend an existing base file, declare an injector in
   `injectors.json` with a marker (e.g. `// devstart:infrastructure-services`)
   and a fragment under `injectors/`.
5. Integration tests run in `tests/DevStart.Cli.Tests/` — new capabilities
   should appear in `GeneratedSourceShapeTests.Variations` so every
   capability combo gets Roslyn-parsed on every CI pass.
6. Submit a PR using the
   [capability-proposal issue template](../.github/ISSUE_TEMPLATE/capability_proposal.yml)
   and an ADR if your capability commits to a new opinion.
