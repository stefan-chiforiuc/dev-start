# Golden path

The five minutes from zero to a running, tested, observable .NET API.

## Path A — Codespaces (recommended)

> Note: the `dev-start-example` template repository is not yet published.
> Until it is, use Path B below.

When the template is published, the flow will be:

1. Use the template to create a new repository.
2. Open it in Codespaces from the **Code → Codespaces** menu.
3. Wait ~60 seconds while the devcontainer pulls and `postCreateCommand`
   runs `just bootstrap` (compose up, migrations, seed, tests).
4. The Codespaces "Ports" tab exposes:
   - `5000` — the API (Scalar docs at `/docs`)
   - `4000` — the local dashboard (links to everything)
   - `5341` — Seq (logs)
   - `16686` — Jaeger (traces)
   - `8080` — Keycloak admin
   - `8025` — Mailhog
   - `9001` — MinIO console

## Path B — Local

Prerequisites: Docker, .NET 8 SDK, [`just`](https://just.systems/),
optionally [`mise`](https://mise.jdx.dev/) for toolchain pinning.

```sh
# install the tool
dotnet tool install -g DevStart

# create a project
dev-start new my-app
cd my-app

# spin everything up
just up

# run the tests
just test

# open the dashboard
open http://localhost:4000
```

`just up` is idempotent — re-running it after a pull is the right thing
to do.

Try:

```sh
curl -s http://localhost:5000/orders | jq
```

You should see seeded orders returned through the full pipeline
(minimal API → MediatR handler → EF Core → Postgres), with a trace in
Jaeger and a structured log in Seq.

## CLI verbs

The same tool covers day 0 and day 300.

| Verb | What it does |
|---|---|
| `dev-start new <name>` | Scaffold a new project (interactive wizard). |
| `dev-start add <capability>` | Add a capability to an existing project. |
| `dev-start doctor` | Diagnose drift, missing env, broken services. |
| `dev-start upgrade` | Open a PR with the delta against the latest template. |
| `dev-start list` | List available capabilities. |

The capability registry lives in
[`capabilities/README.md`](../capabilities/README.md).

## First real change

1. Open `src/My.Application/Orders/Commands/CreateOrder.cs`.
2. Add a field to the command record; FluentValidation will fail the build
   until you also update the validator.
3. Add a migration: `just db.migrate.add AddOrderField`.
4. Run tests: `just test`. Testcontainers spins up a real Postgres,
   applies the migration, runs integration tests.
5. Hit the endpoint from `/.http/orders.http` and watch the trace in Jaeger.

If your project includes the `.claude/` bundle, you can ask Claude Code:

```text
/add-endpoint shipments
```

and it will generate the aggregate, handler, migration, and test following
house style. Review the diff, run `just test`, commit.

## Troubleshooting

- **Ports already in use?** `just doctor` identifies which service is
  bound where.
- **Migrations out of sync?** `just db.reset` drops and recreates the dev DB.
- **Stack won't boot?** `just logs` tails every service. Usually it's
  Keycloak taking ~45 s on first run; be patient.

## What next

- [`paved-road.md`](./paved-road.md) — the opinions, one page.
- [`when-to-leave-the-road.md`](./when-to-leave-the-road.md) — supported
  escape hatches.
- [`adr/`](./adr/) — full rationale for each default.
