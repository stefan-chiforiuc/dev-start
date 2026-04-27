# platform/claude

The default `.claude/` bundle copied into every generated project.

Structure:

- `CLAUDE.md.dotnet.template` / `CLAUDE.md.typescript.template` — briefing
  files, one per stack. `{{ProjectName}}`, `{{CapabilitiesList}}`,
  `{{AdrList}}`, `{{ConditionalServices}}` are filled in at generate time
  by the CLI. `Planner` copies only the matching stack's template; the
  other is not staged.
- `skills/dotnet/`, `skills/typescript/` — markdown skills invoked via
  `/add-endpoint`, `/add-migration`, etc. Stack-split. The `Planner` copies
  only the installed stack's set, stripping the prefix so the file lands
  at `.claude/skills/<name>.md` in the generated project.
- `agents/` — advisory agents (`reviewer`, `architect`) plus stack-specific
  agents under `agents/dotnet/` and `agents/typescript/` (e.g.
  `migration-reviewer`). Called via Claude Code's Task tool.
- `commands/dotnet/`, `commands/typescript/` — slash commands stack-split
  the same way as skills.
- MCP server configs are declared in each capability's `capability.json`
  under `mcp: [{ name, command, args, env }]`. The CLI iterates over the
  installed capabilities to write `.mcp.json`.

## Extension points

Each generated project can:

- Edit its copy freely — it's just markdown and JSON.
- Override a skill by putting the same filename in the project's own
  `.claude/skills/` directory (project overrides platform default).
- Disable a skill by deleting the file; `dev-start doctor` will not
  reinstall it unless `--force`.

## Why ship this in every repo

Because **Claude arrives pre-briefed**. The alternative is every dev
re-explaining the project's conventions at the start of every session.
That doesn't scale, and it silently drifts when conventions change.

With the bundle, conventions are checked in, reviewed like code, and
evolve with the codebase.
