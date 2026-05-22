# DevStart.Cli

The .NET global tool that drives `dev-start`. This README is for
contributors working on the CLI itself. End-user docs (CLI verbs,
workflows) live in [`docs/golden-path.md`](../../docs/golden-path.md).

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

To uninstall a local build:

```sh
dotnet tool uninstall -g DevStart
```
