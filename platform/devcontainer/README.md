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

Generated projects build the devcontainer on first Codespace launch.
Pre-warming via `ghcr.io/dev-start/devcontainer` is under consideration
once usage justifies the publish cadence — track it on the repo issues
list rather than this file.
