#!/usr/bin/env bash
# dev-start sandbox runner.
#
# Builds the CLI from source, runs it against fresh scaffolds in .sandbox/,
# and optionally `dotnet build`s each scaffold so we catch the end-to-end
# class of bugs that pure unit tests can't (broken .sln, missing csproj
# references, malformed templates).
#
# Usage:
#   scripts/sandbox.sh                     # default smoke matrix
#   scripts/sandbox.sh new <name> [args]   # one-off scaffold
#   scripts/sandbox.sh clean               # remove .sandbox/
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SANDBOX_DIR="$REPO_ROOT/.sandbox"
CLI_PROJECT="$REPO_ROOT/src/DevStart.Cli/DevStart.Cli.csproj"
CLI_DLL="$REPO_ROOT/src/DevStart.Cli/bin/Debug/net8.0/dev-start.dll"

# Always surface stack traces in the sandbox so failures are diagnosable.
export DEV_START_DEBUG=1

build_cli() {
  echo "::: building CLI"
  dotnet build "$CLI_PROJECT" -c Debug --nologo --verbosity quiet
}

run_scaffold() {
  local name="$1"; shift
  local slug="${name//./-}"
  slug="${slug,,}"
  local cell_dir="$SANDBOX_DIR/$slug"
  rm -rf "$cell_dir"
  mkdir -p "$cell_dir"
  echo "::: scaffolding $name -> $cell_dir"
  (cd "$cell_dir" && dotnet "$CLI_DLL" new "$name" --no-interactive --no-claude "$@")
}

build_scaffold() {
  local name="$1"
  local slug="${name//./-}"
  slug="${slug,,}"
  # The scaffolded project name preserves dots; the folder name matches the
  # solution name (set in Planner). Discover whichever directory was created.
  local project_dir
  project_dir="$(find "$SANDBOX_DIR/$slug" -mindepth 1 -maxdepth 1 -type d | head -n1)"
  if [ -z "$project_dir" ]; then
    echo "::: no project directory found under $SANDBOX_DIR/$slug" >&2
    return 1
  fi
  echo "::: dotnet build $project_dir"
  (cd "$project_dir" && dotnet build --nologo --verbosity quiet)
}

smoke_matrix() {
  build_cli
  rm -rf "$SANDBOX_DIR"
  mkdir -p "$SANDBOX_DIR"

  # Each cell exercises a different historical bug surface:
  #   plain        — baseline kebab name
  #   dotted       — Some.Project.Name preservation (bug 1)
  #   multi+gw     — gateway auto-register (bug 3)
  #   pascal       — PascalCase input round-trips cleanly
  run_scaffold "smoke-plain"
  run_scaffold "My.Cool.App" --with postgres
  run_scaffold "multi-gateway" --multi-service
  run_scaffold "PascalApp"

  for name in smoke-plain My.Cool.App multi-gateway PascalApp; do
    build_scaffold "$name"
  done

  echo "::: sandbox smoke OK"
}

case "${1:-smoke}" in
  smoke) smoke_matrix ;;
  new)
    shift
    build_cli
    mkdir -p "$SANDBOX_DIR"
    run_scaffold "$@"
    ;;
  build)
    shift
    build_scaffold "$@"
    ;;
  clean)
    rm -rf "$SANDBOX_DIR"
    echo "::: removed $SANDBOX_DIR"
    ;;
  *)
    echo "usage: $0 {smoke|new <name> [args]|build <name>|clean}" >&2
    exit 64
    ;;
esac
