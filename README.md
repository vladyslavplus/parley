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

## Demo workspace

The demo creates the organization "Parley Demo" with about 5,900 generated customers,
their orders, deals, tasks, notes and timeline. The customers come from the Online
Retail II dataset (CC BY 4.0, attribution in `ml/datacards/online_retail_ii.md`). The
demo uses its own database, `parley_demo`, never your everyday database.

1. Start the support services, then a throwaway database container on a free port:

   ```powershell
   docker compose up -d
   docker run -d --name parley-demo-db -e POSTGRES_PASSWORD=<choose-a-password> -p 127.0.0.1:55440:5432 pgvector/pgvector:0.8.7-pg17
   ```

2. Build the demo export (the ML environment is described in the ML section):

   ```powershell
   uv run --directory ml python -m parley_ml.datasets.online_retail --config configs/online_retail.yaml
   uv run --directory ml python -m parley_ml.datasets.demo_export --config configs/demo_export.yaml
   ```

3. Set the connection string for the demo database. Keep it in the environment, not on the command line:

   ```powershell
   $env:PARLEY_DEMO_CONNECTION_STRING = "Host=127.0.0.1;Port=55440;Database=parley_demo;Username=postgres;Password=<choose-a-password>"
   ```

4. Drop and recreate `parley_demo`, apply the migrations and seed it. The script refuses any
   database that is not named `parley_demo` or `parley_demo_<suffix>`, any host other than
   the local machine, and the port of the compose database (5433):

   ```powershell
   scripts\reset-demo.ps1 -ConfirmReset
   ```

5. Start the API on the demo database. The variable overrides the Development setting, and the
   API applies any pending migrations when it starts:

   ```powershell
   $env:Database__ConnectionString = $env:PARLEY_DEMO_CONNECTION_STRING
   dotnet run --project backend/src/Parley.Api
   ```

6. Start the web app. Its API address is set in `frontend/.env.local` (`API_BASE_URL`):

   ```powershell
   pnpm --dir frontend dev
   ```

The demo accounts are written to `artifacts/demo-accounts.txt` in the repository root. That
folder is git-ignored, so the passwords are never committed. Do not copy them into tracked
files. The accounts are `admin`, `marketer`, `viewer`, two managers (`manager.north`,
`manager.south`), and six sales reps (`rep.north.1` to `rep.north.3`, `rep.south.1` to
`rep.south.3`), all on the domain `demo.parley.test`. If the file is lost, issue new
passwords with the same seed command and `--reset-passwords`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__ConnectionString = $env:PARLEY_DEMO_CONNECTION_STRING
dotnet run --project backend/src/Parley.Api --no-launch-profile -- seed-demo --input ml/artifacts/demo_export --confirm-target-database --reset-passwords
```

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
