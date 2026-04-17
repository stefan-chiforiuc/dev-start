# devcontainer

The shared devcontainer used by generated projects and by this repo itself.

Build it locally:

```sh
docker build -t ghcr.io/dev-start/devcontainer:dev -f Dockerfile .
```

Features included:

- .NET 8 SDK (base image)
- Node (for the `sdk` capability and docs tooling)
- Docker-in-Docker (so `just up` works inside Codespaces)
- `just` (task runner)
- `mise` (toolchain pinning via `.tool-versions`)

We publish a pre-warmed image to `ghcr.io/dev-start/devcontainer` via a
release workflow (TODO) so that Codespaces start times are under 60s.
