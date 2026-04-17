# platform/claude

The default `.claude/` bundle copied into every generated project.

Structure:

- `CLAUDE.md.template` — briefing file. `{{ProjectName}}`, `{{CapabilitiesList}}`,
  `{{AdrList}}`, `{{ConditionalServices}}` are filled in at generate time by
  the CLI.
- `skills/` — markdown skills invoked via `/add-endpoint`, `/add-migration`,
  etc. Copied verbatim into the generated project's `.claude/skills/`.
- `agents/` — advisory agents (`reviewer`, `migration-reviewer`,
  `architect`). Called via Claude Code's Task tool.
- `mcp-servers/` — MCP server configs pointing at the live compose stack
  (`postgres`, `seq-logs`). Merged into the user's `.claude/settings.json`.

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
