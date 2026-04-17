# DevStart.Cli

The .NET global tool that drives `dev-start`.

## Build

```sh
dotnet build src/DevStart.Cli
```

## Pack

```sh
dotnet pack src/DevStart.Cli -c Release -o artifacts
```

## Install locally

```sh
dotnet tool install -g --add-source ./artifacts DevStart
dev-start --help
```

## Commands

- `dev-start new <name>` — scaffold a new project (stubbed in v0.1).
- `dev-start add <cap>` — add a capability to an existing project
  (manifest update only in v0.1).
- `dev-start doctor` — diagnose a project for drift and missing services.
- `dev-start upgrade` — diff against latest templates and produce a
  patch (stubbed in v0.1).
- `dev-start list` — list available capabilities.

## v0.1 scope

The CLI shell is wired, the capability manifest parser is wired, and
`dev-start list` + `dev-start doctor` are functional against a
hand-written `.devstart.json`. File copy, token replacement, patch
application, and git-init are stubbed for v0.2 so that the overall
architecture is visible and reviewable first.
