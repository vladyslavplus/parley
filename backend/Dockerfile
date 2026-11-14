# Multi-stage build of the API. The context is `backend/`, so `bin`, `obj` and `TestResults` stay out through
# `.dockerignore`. Restore uses the committed lock files in locked mode; the runtime image carries the published app only,
# no SDK, no source and no secrets (every setting arrives through the environment at run time).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# The build configuration and the solution first, so a source-only change reuses the restore layer.
COPY global.json Directory.Build.props Directory.Packages.props Parley.slnx .editorconfig ./
COPY src/ ./src/
COPY tests/ ./tests/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore Parley.slnx --locked-mode

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/Parley.Api/Parley.Api.csproj --configuration Release --no-restore \
    --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# The .NET images define APP_UID for the non-root `app` user; the process runs as that user, never as root.
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

# Contact exports are written under CrmExports:Directory (relative to the content root).
RUN mkdir -p /app/crm-exports && chown $APP_UID:$APP_UID /app/crm-exports

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "Parley.Api.dll"]
