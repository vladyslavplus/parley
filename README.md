# Parley

Parley is a multi-tenant customer relationship management (CRM) system built
as a modular monolith with an ASP.NET Core API and a Next.js frontend. An
optional ML service provides uplift and customer lifetime value scoring.

## Prerequisites

- .NET 10 SDK (10.0.401)
- Docker Desktop with Compose

## Local setup

Committed configuration holds placeholders only (`<db_host>`, `<auth_signing_key>`
and so on). The API refuses to start until every placeholder is replaced. Local
values live in git-ignored files that the tracked examples describe.

1. Create the Compose environment file and set the local values from its comments:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Create the local port overrides. They publish the services on `127.0.0.1` only:

   ```powershell
   Copy-Item docker-compose.override.example.yml docker-compose.override.yml
   ```

3. Create the API development settings and replace each placeholder with the local
   value (the database user and password must match `.env`):

   ```powershell
   Copy-Item backend/src/Parley.Api/appsettings.Development.example.json backend/src/Parley.Api/appsettings.Development.json
   ```

4. Start the containers:

   ```powershell
   docker compose up -d
   ```

With the override in place, PostgreSQL with vector search is on `127.0.0.1:5433`,
Mailpit is on `127.0.0.1:8025` (SMTP `127.0.0.1:1025`), and the Aspire Dashboard is
on `127.0.0.1:18888` (OpenTelemetry on `127.0.0.1:4317`). Enable the optional cache
with `docker compose --profile redis up -d`.

`docker compose` on its own (base file, no override) publishes no ports, so the
services are reachable only from other containers.

## Run the API

```powershell
dotnet run --project backend/src/Parley.Api
```

The API listens on `http://localhost:5000`. The health endpoint is
`GET /health`, the OpenAPI document is `GET /openapi/v1.json`, and the Scalar
reference is at `http://localhost:5000/scalar/v1` in Development.

Settings resolve in this order: `appsettings.json` (placeholders), then
`appsettings.Development.json` in the Development environment, then environment
variables such as `Auth__SigningKey` (the highest priority).

## Rate limits behind the web BFF

The API rate-limits by client address. Browsers reach it through the Next.js BFF, so
the BFF address must be listed in the API setting `TrustedProxies` (environment form
`TrustedProxies__KnownProxies__0=<address>`); otherwise all users share one bucket.
The BFF forwards the end-user address in `X-Forwarded-For` only when it runs behind
an ingress that appends the real client address and `TRUSTED_INGRESS=true` is set
(`frontend/.env.example`). Local development settings trust the loopback addresses.

## Build and test

```powershell
dotnet build backend/Parley.slnx -warnaserror
dotnet test backend/tests/Parley.UnitTests
dotnet test backend/tests/Parley.IntegrationTests
```

Integration tests start a disposable PostgreSQL container with the same image
as the local stack, so Docker must be running. They supply their own settings
and do not read the git-ignored Development file, so they run on a clean clone.

## Transactional email

Account emails (confirmation and password reset) are sent through
`ITransactionalEmailSender` with MailKit SMTP and Fluid templates in `uk`
and `en`. The caller passes the recipient culture explicitly. Known cultures
are `uk` and `en`; any other value, including an empty value, falls back to
`Email:DefaultCulture`.

Local development uses Mailpit. In the `Development` environment the API
connects to `127.0.0.1:1025`, so `docker compose up -d` is enough to send
and inspect messages at `http://127.0.0.1:8025`.

Configuration is bound from `appsettings.json` and
`appsettings.Development.json` and overridable by environment variables:

| Environment variable | Configuration key |
| --- | --- |
| `Email__Host` | `Email:Host` |
| `Email__Port` | `Email:Port` |
| `Email__FromAddress` | `Email:FromAddress` |
| `Email__FromName` | `Email:FromName` |
| `Email__SecureSocketOption` | `Email:SecureSocketOption` |
| `Email__Username` | `Email:Username` |
| `Email__Password` | `Email:Password` |
| `Email__Timeout` | `Email:Timeout` |
| `Email__DefaultCulture` | `Email:DefaultCulture` |

Real SMTP credentials are never committed: set `Email__Username` and
`Email__Password` from the deployment environment. Send attempts log only
the template key and culture, never the recipient address or body.

## Frontend

```powershell
Copy-Item frontend/.env.example frontend/.env.local
pnpm --dir frontend install
pnpm --dir frontend check
pnpm --dir frontend test
pnpm --dir frontend build
pnpm --dir frontend dev
```

## ML

```powershell
uv run --directory ml pytest
docker build -f ml/Dockerfile.train -t parley-ml-train ml
docker run --rm --gpus all parley-ml-train python -m parley_ml.gpu_check
```
