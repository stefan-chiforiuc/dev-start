# dev-start-dotnet — self-hosted justfile.
# Used by contributors working on this repo.

set shell := ["bash", "-cu"]

default:
    @just --list

build:
    dotnet build DevStartDotnet.sln --configuration Debug

test:
    dotnet test DevStartDotnet.sln --configuration Debug

fmt:
    dotnet format DevStartDotnet.sln

lint:
    dotnet format DevStartDotnet.sln --verify-no-changes

pack:
    dotnet pack src/DevStartDotnet.Cli/DevStartDotnet.Cli.csproj -c Release -o artifacts

install-local:
    just pack
    dotnet tool uninstall -g DevStartDotnet 2>/dev/null || true
    dotnet tool install -g --add-source ./artifacts DevStartDotnet

# Install pre-commit hooks: gitleaks + dotnet format + markdownlint.
# Reads from platform/hooks/pre-commit so the installed hook stays in sync
# with the version reviewed in code. Refuses to overwrite an unrelated hook.
install-hooks:
    #!/usr/bin/env bash
    set -euo pipefail
    src="platform/hooks/pre-commit"
    dst="$(git rev-parse --git-path hooks)/pre-commit"
    if [ -e "$dst" ] && ! grep -q "dev-start-dotnet: managed pre-commit" "$dst"; then
      echo "Refusing to overwrite existing $dst." >&2
      echo "Move it aside or merge its contents, then re-run 'just install-hooks'." >&2
      exit 1
    fi
    install -m 0755 "$src" "$dst"
    echo "Installed $dst (sourced from $src)."
    echo "Tools used: gitleaks, dotnet, markdownlint-cli2 (install any that are missing)."

list-caps:
    dotnet run --project src/DevStartDotnet.Cli -- list
