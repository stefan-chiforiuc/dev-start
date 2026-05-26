# Bug catalog

A living list of bugs we've fixed in `dev-start`, kept so reviewers and
agents can pattern-match against PRs that touch the same surfaces. Append
new entries on every bug fix. Format: symptom → root cause → where to look
→ regression guard.

This file is reviewed against ADR
[0011 — Capability Definition of Done](adr/0011-capability-definition-of-done.md).
If you're about to merge a change that touches one of the affected files
below, re-read the catalog entry first.

---

## BUG-001 — Dotted .NET names mangled

- **Symptom.** `dev-start new My.Cool.App` produced folder `my-cool-app/`
  and solution `MyCoolApp.sln` instead of folder `My.Cool.App/` and
  `My.Cool.App.sln`. Dotted .NET namespaces were silently flattened.
- **Root cause.** `Tokens.Normalize` replaced `.` with `-`, then
  `ToPascal` split on `-` and concatenated — so segments were stripped
  before `Name` was built. The same `Name` is used for sln/csproj/namespace
  in ~100 capability files, so the loss cascaded.
- **Where to look.** `src/DevStart.Cli/Tokens.cs`; `Planner.RunAsync`
  picking `Tokens.KebabName` for the folder.
- **Regression guard.** `TokensTests.Preserves_dotted_dotnet_namespaces`;
  sandbox smoke matrix includes `My.Cool.App`.

## BUG-002 — Wizard appears to hang after capability selection

- **Symptom.** After picking capabilities, the terminal sat silent for
  several seconds before the `Done.` line appeared.
- **Root cause.** Multi-second I/O phases — platform bundle copy, Claude
  bundle copy (~50 embedded files), MCP-config write, manifest write,
  baselines save, `git init` / `git add -A` / `git commit` — emitted no
  console output. `git commit` also had no timeout and could block forever
  on a credential or signing prompt.
- **Where to look.** `src/DevStart.Cli/Planner.cs` `Render` and `Run` (the
  `Process.WaitForExit()` call).
- **Regression guard.** Each phase now emits a `[cyan]· {phase}[/]` status
  line; `Run` has a 30-second timeout and sets `GIT_TERMINAL_PROMPT=0`.

## BUG-003 — Gateway "couldn't auto-register" warning

- **Symptom.** Adding the `gateway` capability surfaced
  `warn couldn't auto-register Gateway.csproj in the solution; run dotnet
  sln add manually`.
- **Root cause.** Not gateway-specific. `CapabilityInstaller.TryRegisterInSolution`
  shells out to `dotnet sln add` from the project root. The scaffolded
  `global.json` pinned `8.0.404` with `rollForward: latestFeature` — which
  does *not* roll backward — so any contributor without exactly that SDK
  hit a 145-exit subprocess and the warning. Gateway was just the first
  capability that added a `.csproj` outside the pre-registered sln, so it
  was the user-visible surface.
- **Where to look.** `capabilities/base/files/global.json`,
  `capabilities/base-aspnet-9/files/global.json`,
  `src/DevStart.Cli/CapabilityInstaller.cs:TryRegisterInSolution`.
- **Regression guard.** Scaffolded `global.json` now pins to a minimum
  patch (`8.0.100` / `9.0.100`) with `latestFeature` roll-forward — any
  installed 8.x / 9.x SDK satisfies it. Sandbox smoke `multi-gateway` cell
  asserts no warning + `dotnet build` is green.

## BUG-004 — Output told users to run `devstart` instead of `dev-start`

- **Symptom.** Several command outputs (`doctor`, `install`, hints in
  `add`) suggested `devstart install` / `devstart doctor`, but the actual
  CLI binary is `dev-start`. Copy-pasting failed.
- **Root cause.** Drift between the package id (`DevStart`, no dash) and
  the executable name (`dev-start`, with dash). User-facing strings used
  the wrong form.
- **Where to look.** `src/DevStart.Cli/Commands/*.cs` for any quoted
  `devstart <verb>` form.
- **Regression guard.** `UserFacingTextTests.No_user_facing_string_says_devstart_without_dash`
  scans the CLI source for `devstart <verb>` and fails the build if any
  reappears.

## BUG-005 — Raw stack traces on user errors

- **Symptom.** Invalid project names (`bad..name`, leading-dash, empty)
  printed a 40-line System.CommandLine stack trace instead of a friendly
  message.
- **Root cause.** `Program.Main` invoked `RootCommand.InvokeAsync(args)`
  with no exception handling. System.CommandLine's default middleware
  caught exceptions but rendered them as raw `Unhandled exception:` blocks.
  User-friendly errors (`ArgumentException`) were indistinguishable from
  programmer bugs.
- **Where to look.** `src/DevStart.Cli/Program.cs`,
  `src/DevStart.Cli/Exceptions.cs`.
- **Regression guard.** New `DevStartUserException(message, hint)` type;
  validation paths throw it; `Program.cs` uses
  `CommandLineBuilder.UseExceptionHandler` to render it as
  `error: <message>` + optional `hint:` line, with stacks gated behind
  `DEV_START_DEBUG=1`.

## BUG-006 — "Done. Next:" printed twice

- **Symptom.** Cosmetic — the end-of-`new` summary appeared twice with
  conflicting instructions.
- **Root cause.** Both `Planner.RunAsync` and `NewCommand` were printing a
  closing block. Likely added in different PRs without anyone noticing.
- **Where to look.** End of `src/DevStart.Cli/Planner.cs:RunAsync`,
  end of `src/DevStart.Cli/Commands/NewCommand.cs`.
- **Regression guard.** `Planner.RunAsync` no longer prints the closing
  block; `NewCommand` owns it. Caught next by sandbox smoke (visual diff
  on the captured log).

## BUG-007 — Repo `global.json` pinned 8.0.404 (contributors couldn't build)

- **Symptom.** Fresh clone + apt-installed `dotnet-sdk-8.0` (which gives
  8.0.127 on Ubuntu 24.04) refused to build with "Requested SDK version:
  8.0.404 ... global.json file: .../global.json".
- **Root cause.** Pinned to a specific feature-band patch with
  `rollForward: latestFeature` (which does not roll backward).
- **Where to look.** `/global.json`.
- **Regression guard.** Repo `global.json` now pins to `8.0.100` (allows
  any installed 8.x SDK ≥ first feature band). Mirrored in the scaffolded
  `global.json` (BUG-003).

---

## How to add an entry

When you fix a bug, append a `BUG-NNN` block here in the same shape:
symptom → root cause → where to look → regression guard. The regression
guard line must reference an actual test or smoke check that exists in
the repo — no entry should rely on tribal knowledge.
