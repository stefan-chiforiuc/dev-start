# dev-start — self-hosted justfile.
# Used by contributors working on this repo.

set shell := ["bash", "-cu"]

default:
    @just --list

build:
    dotnet build DevStart.sln --configuration Debug

test:
    dotnet test DevStart.sln --configuration Debug

fmt:
    dotnet format DevStart.sln

lint:
    dotnet format DevStart.sln --verify-no-changes

pack:
    dotnet pack src/DevStart.Cli/DevStart.Cli.csproj -c Release -o artifacts

# Pack with an explicit version, mirroring what release-please does in CI.
# Use a prerelease suffix (e.g. 1.0.0-alpha.dev) when testing locally.
pack-version VERSION:
    dotnet pack src/DevStart.Cli/DevStart.Cli.csproj \
      -c Release -p:Version={{VERSION}} -o artifacts

install-local:
    just pack
    dotnet tool uninstall -g DevStart 2>/dev/null || true
    dotnet tool install -g --add-source ./artifacts DevStart

# Install a specific stamped version locally (works with prerelease suffixes).
install-version VERSION:
    just pack-version {{VERSION}}
    dotnet tool uninstall -g DevStart 2>/dev/null || true
    dotnet tool install -g --add-source ./artifacts DevStart \
      --prerelease --version {{VERSION}}

# Dry-run release-please against this repo without creating or updating a PR.
# Requires GITHUB_TOKEN in the environment (a fine-grained PAT with read access is fine).
release-dry-run:
    npx --yes release-please release-pr \
      --token "$GITHUB_TOKEN" \
      --repo-url stefan-chiforiuc/dev-start \
      --config-file .github/release-please-config.json \
      --manifest-file .github/release-please-manifest.json \
      --dry-run

# Install pre-commit hooks: gitleaks + dotnet format + markdownlint.
# Reads from platform/hooks/pre-commit so the installed hook stays in sync
# with the version reviewed in code. Refuses to overwrite an unrelated hook.
install-hooks:
    #!/usr/bin/env bash
    set -euo pipefail
    src="platform/hooks/pre-commit"
    dst="$(git rev-parse --git-path hooks)/pre-commit"
    if [ -e "$dst" ] && ! grep -q "dev-start: managed pre-commit" "$dst"; then
      echo "Refusing to overwrite existing $dst." >&2
      echo "Move it aside or merge its contents, then re-run 'just install-hooks'." >&2
      exit 1
    fi
    install -m 0755 "$src" "$dst"
    echo "Installed $dst (sourced from $src)."
    echo "Tools used: gitleaks, dotnet, markdownlint-cli2 (install any that are missing)."

list-caps:
    dotnet run --project src/DevStart.Cli -- list
