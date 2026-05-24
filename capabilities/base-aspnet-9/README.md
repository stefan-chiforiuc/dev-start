# `base-aspnet-9`

ASP.NET Core 9 foundation. Same layered shape as `base` (ASP.NET 8) —
shares 30 template files via `extends: "_shared/backend-aspnet"`, ships
only the 5 version-pinned files (`Directory.Build.props`,
`Directory.Packages.props`, `global.json`, `Dockerfile`,
`.vscode/launch.json`).

## How to pick it

Wizard route — `dev-start new my-app`, then pick `aspnet` →  `9`.
Flag route — `dev-start new my-app --framework aspnet --framework-version 9 --no-interactive`.

## What changes vs. `base`

- `<TargetFramework>net9.0</TargetFramework>` (was `net8.0`).
- SDK pin in `global.json` bumped to `9.0.x`.
- NuGet versions bumped to .NET 9 majors where available (EF Core 9,
  ASP.NET 9 packages, OpenAPI 9). Cross-cutting libs (MediatR,
  FluentValidation, MassTransit, AWSSDK, Yarp) stay on their latest
  cross-version release.
- Docker base images on `9.0-bookworm-slim` tags.

Everything else (layered projects, Program.cs, justfile, CI workflows)
comes from the shared overlay.
