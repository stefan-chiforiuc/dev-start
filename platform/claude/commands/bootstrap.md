---
description: Bring up the local stack, migrate, and verify the golden path
---

Bring the project up from a cold start and verify it's working:

1. Run `just up` to boot the compose stack.
2. Wait for Postgres: `until docker compose exec -T postgres pg_isready -U dev -d app; do sleep 1; done`.
3. Apply migrations: `just db-migrate` (or skip if none yet).
4. Run `just test` and report any failures.
5. Curl the seeded endpoint: `curl -s http://localhost:5000/v1/orders | head`.
6. Open the dashboard and summarise which services are green.

If any step fails, don't retry blindly — diagnose the root cause and
propose a fix.
