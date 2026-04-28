# DevStart.Cli

The .NET global tool that drives `dev-start`. Published to NuGet as
[`DevStart`](https://www.nuget.org/packages/DevStart).

For end-user docs (`new`, `add`, `doctor`, `upgrade`, `promote`, `policy`,
the capability table, the paved road), see the [repo
README](../../README.md). This file covers building the tool from source.

## Build

```sh
dotnet build src/DevStart.Cli
```

## Pack

```sh
dotnet pack src/DevStart.Cli -c Release -o artifacts
```

The package version is **stamped at pack time** via `-p:Version=...`. Without
it, you get a `1.0.0` dev build. The release pipeline
(`.github/workflows/release-please.yml`) sets the version from
`.github/release-please-manifest.json`. See [`RELEASING.md`](../../RELEASING.md).

## Install locally

From the repo root:

```sh
just install-local        # pack + uninstall any global tool + install fresh
dev-start --version       # confirm
dev-start --help
```

## Project layout

```text
src/DevStart.Cli/
  Program.cs                # System.CommandLine root
  Commands/                 # one file per CLI verb
  Capability.cs             # capability metadata + discovery
  CapabilityInstaller.cs    # apply files + injectors to a target project
  Planner.cs                # resolve dependencies, gate on stack
  Manifest.cs               # .devstart.json schema + migrations
  Tokens.cs                 # {{Name}} / {{name}} / {{nameCamel}} substitution
  Injector.cs               # marker / anchor-based file extension
  JsonMerger.cs             # json-merge mode for package.json / tsconfig.json
  Policy.cs                 # org policy bundles
  Baselines.cs              # .devstart/baselines.json for upgrade --apply
  Upgrader.cs               # 3-way merge for upgrade --apply
  EmbeddedResourceIndex.cs  # shared index over capabilities/ and policies/
  CliVersion.cs             # AssemblyInformationalVersion → --version
```

Capability and platform bundles are embedded as MSBuild `<EmbeddedResource>`
items so the packed `.nupkg` is self-contained. See the `<ItemGroup>` in
`DevStart.Cli.csproj`.
