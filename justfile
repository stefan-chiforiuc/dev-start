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

install-local:
    just pack
    dotnet tool uninstall -g DevStart 2>/dev/null || true
    dotnet tool install -g --add-source ./artifacts DevStart

list-caps:
    dotnet run --project src/DevStart.Cli -- list
