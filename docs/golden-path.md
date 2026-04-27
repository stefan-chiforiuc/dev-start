# Golden path

The few minutes from zero to a running, tested, observable API.

## Local

Prerequisites: Docker, .NET 8 SDK (or Node 20+ for the TypeScript stack),
[`just`](https://just.systems/), optionally
[`mise`](https://mise.jdx.dev/) for toolchain pinning.

```sh
# install the tool
# while 1.0.0-alpha is the current release, --prerelease is required.
# drop the flag once 1.0.0 (non-alpha) is published.
dotnet tool install -g DevStart --prerelease

# create a project (.NET stack is the default; pass --stack typescript for TS)
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

Once running, services are exposed at:

- `5000` — the API (Scalar docs at `/docs`)
- `4000` — the local dashboard (links to everything)
- `5341` — Seq (logs)
- `16686` — Jaeger (traces)
- `8080` — Keycloak admin
- `8025` — Mailhog
- `9001` — MinIO console

Try:

```sh
curl -s http://localhost:5000/orders | jq
```

You should see seeded orders returned through the full pipeline
(minimal API → MediatR handler → EF Core → Postgres), with a trace in
Jaeger and a structured log in Seq.

## Codespaces

A "Use this template" + Codespaces flow is planned but the published
template repository isn't live yet. Track this on the issues list.

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

- `docs/paved-road.md` — the opinions, one page.
- `docs/when-to-leave-the-road.md` — supported escape hatches.
- `docs/adr/` — full rationale for each default.
