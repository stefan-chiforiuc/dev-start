#!/bin/bash
# Claude Code on the Web — session start hook for dev-start.
#
# Cloud containers start fresh: no .NET SDK, no `just`, no NuGet cache.
# Without this hook, the agent's first ~2 minutes are spent diagnosing why
# `dotnet` isn't on PATH (real story — see this repo's BUG-007).
#
# Local sessions skip the install — assumes you already have the toolchain.
# Idempotent: each `command -v` check short-circuits when the binary exists.
set -euo pipefail

# Only run in remote / Claude Code on the Web. Local users manage their own SDK.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REPO_ROOT="${CLAUDE_PROJECT_DIR:-$(pwd)}"

echo "::: dev-start session-start hook"

# .NET SDK 8 — read global.json to know if we need it. The repo's global.json
# pins to a minimum patch (8.0.100) with rollForward: latestFeature, so any
# 8.x SDK Ubuntu apt provides (currently 8.0.127) will satisfy it.
if ! command -v dotnet >/dev/null 2>&1; then
  echo "::: installing dotnet-sdk-8.0 (apt)"
  sudo apt-get update -qq
  sudo apt-get install -y --no-install-recommends dotnet-sdk-8.0 >/dev/null
else
  echo "::: dotnet present: $(dotnet --version)"
fi

# `just` — used by `just build`, `just test`, `just sandbox`. Ubuntu 24.04
# ships it in `universe`. Cheap install (single binary).
if ! command -v just >/dev/null 2>&1; then
  echo "::: installing just (apt)"
  sudo apt-get install -y --no-install-recommends just >/dev/null 2>&1 || {
    echo "::: apt 'just' unavailable; falling back to cargo/snap is out of scope — skipping"
  }
fi

# Warm the NuGet cache so the first `dotnet build` / `dotnet test` doesn't
# block on package download. Container state is cached after the hook, so
# this pays off on every subsequent invocation in the same session image.
if command -v dotnet >/dev/null 2>&1 && [ -f "$REPO_ROOT/DevStart.sln" ]; then
  echo "::: dotnet restore"
  ( cd "$REPO_ROOT" && dotnet restore DevStart.sln --nologo --verbosity quiet ) || true
fi

echo "::: session-start hook done"
