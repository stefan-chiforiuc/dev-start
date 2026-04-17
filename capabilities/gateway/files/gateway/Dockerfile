# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
COPY . ./
RUN dotnet publish gateway/Gateway.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
USER app
ENTRYPOINT ["dotnet", "Gateway.dll"]
