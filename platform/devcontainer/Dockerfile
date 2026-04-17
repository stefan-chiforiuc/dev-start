# syntax=docker/dockerfile:1.7
# Pre-warmed devcontainer for dev-start-generated projects.

FROM mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm

# just — cross-platform task runner.
RUN curl --proto '=https' --tlsv1.2 -sSf https://just.systems/install.sh \
    | bash -s -- --to /usr/local/bin

# mise — toolchain pinning.
RUN curl https://mise.run | sh && \
    mv /root/.local/bin/mise /usr/local/bin/mise

# Common .NET global tools cached into the image.
RUN dotnet tool install -g dotnet-ef && \
    dotnet tool install -g dotnet-outdated-tool

ENV PATH="${PATH}:/root/.dotnet/tools"

# Pre-pull common NuGet packages (cached layer; best-effort).
# Overlay with the user's packages on postCreateCommand.
RUN mkdir -p /nuget-warm && \
    cd /nuget-warm && \
    dotnet new webapi --no-https --framework net8.0 -o warm && \
    cd warm && \
    dotnet restore && \
    cd / && rm -rf /nuget-warm

WORKDIR /workspaces
