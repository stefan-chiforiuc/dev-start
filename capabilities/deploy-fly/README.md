# deploy-fly capability

Fly.io deploy recipe. Ships `fly.toml` and a GitHub Actions deploy
workflow that runs on tag pushes.

## Wires

- `fly.toml` pointing at the generated Dockerfile.
- `.github/workflows/deploy-fly.yml` — deploys on tag `v*` after CI is green.
- Documents `FLY_API_TOKEN` as a required GitHub secret.

## Opinions

- **Single region by default** (`iad`). Change per team need.
- **HTTP service** on 5000 — matches the generated Dockerfile.
- **Auto rollout** on deploy workflow success; no manual promotion.

## Escape hatches

- Switch to `flyctl deploy --remote-only` in the workflow if local builds
  are preferred.
- Multi-region — add extra `primary_region` + `[[services]]` entries in
  `fly.toml`.
