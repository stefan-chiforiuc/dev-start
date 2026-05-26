---
description: Run the end-to-end sandbox smoke matrix and report green/red.
allowed-tools: Bash
---

Run `just sandbox` (which builds the CLI, scaffolds the smoke matrix —
`smoke-plain`, `My.Cool.App`, `multi-gateway`, `PascalApp` — and
`dotnet build`s each scaffolded project under `.sandbox/`).

Output a one-line summary first:
- **Green** — `sandbox smoke OK`. End with that, no extra commentary.
- **Red** — name the failed cell (e.g. "multi-gateway scaffold failed"
  or "PascalApp dotnet build failed"), then show the last ~20 lines of
  the relevant log section so the user can diagnose. Don't try to fix it
  unless the user asks — this command is verification, not repair.

Run `just sandbox` directly. If `just` is not on PATH, run
`./scripts/sandbox.sh smoke` as the fallback. Do not invoke `dotnet`
directly — the script handles build + matrix + verify.
