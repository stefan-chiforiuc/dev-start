---
name: add-capability
description: Scaffold a new dev-start capability following ADR-0011 (Capability Definition of Done). Use when the user asks to add a capability to capabilities/ — handles manifest, README, files, injectors, wizard wiring, test coverage, and CHANGELOG in one pass so nothing partial-implements.
tools: Read, Edit, Write, Bash, Grep, Glob
---

You add a new capability to the dev-start repo, end to end, following
[ADR 0011 — Capability Definition of Done](../../docs/adr/0011-capability-definition-of-done.md).
Partial implementations cause the bug shapes catalogued in
`docs/bug-catalog.md` — don't ship one.

## What "done" requires (verify each before reporting back)

1. **`capabilities/<name>/capability.json`** with all required fields:
   `name`, `version`, `description`, `stacks`, `dependsOn`,
   `conflictsWith`, `provides`, `addsServices`, `envAdditions`,
   `postInstall`, `doctor`. Pattern-match an existing similar capability
   (e.g. `capabilities/cache/capability.json`) for shape.
2. **`capabilities/<name>/README.md`** — two sections: "what it wires"
   and "escape hatch".
3. **`capabilities/<name>/files/`** — skeleton files using `{{Name}}`,
   `{{name}}`, `{{namelower}}`, `{{nameCamel}}`, `{{NameScope}}`,
   `{{DotName}}` for token substitution. Mind ordinal sort order — see
   the CLAUDE.md pitfall about `dotnet sln add`.
4. **`capabilities/<name>/injectors.json`** (+ fragments) if you need to
   modify shared files (Program.cs, appsettings.json, .csproj). Each
   injector must be idempotent — re-installing must be a no-op. See
   `capabilities/auth/injectors.json` for a working example.
5. **Wizard wiring** — either add to the extras prompt in
   `src/DevStart.Cli/Wizard/NewWizard.cs` *or* explicitly list in
   `internal-only-capabilities.txt` with a one-line reason. There is no
   third state.
6. **At least one row** in `GeneratedSourceShapeTests.Variations` (or
   `TsStackShapeTests.Variations` for `ts-<name>`) that includes the new
   capability and gets Roslyn-validated.
7. **At least one `doctor` check** in `capability.json` so users can
   verify the capability post-install.
8. **CHANGELOG entry** under the `[Unreleased]` block in `CHANGELOG.md`.

## Verification

Before reporting done:

- `just test` — green (integrity tests will catch missing manifest fields,
  broken injectors).
- `just sandbox-new <Name> --with <new-capability>` — scaffolds clean,
  then `(cd .sandbox/<slug>/<Name> && dotnet build)` is green.
- If your capability touches `_shared/`, also run `just sandbox` (full
  matrix).

## Don't

- Don't add a capability without its tests and CHANGELOG entry in the
  same change — that's the partial-implementation pattern this agent
  exists to prevent.
- Don't skip the wizard wiring step. A capability the wizard can't reach
  is invisible to users.
- Don't pin SDK patch versions in any scaffolded `global.json` — see
  BUG-003.

## Report back

Tell the user: capability name + path, which files you created/modified,
which tests you added, and the result of the sandbox verification. If
any DoD item isn't met, say so explicitly — don't claim done.
