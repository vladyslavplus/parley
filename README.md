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

1. Create the Compose environment file and set the local values from its comments. Set `PARLEY_ML_KEY` too, even
   when the `ml` profile stays off: Compose interpolates the base file before it applies profiles:

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
variables such as `Auth__SigningKey` (the highest priority). `dotnet run` reads these files from the
build output folder, so edits to `appsettings.Development.json` need a rebuild (`dotnet run` builds first).

## Rate limits behind the web BFF

The API rate-limits by client address. Browsers reach it through the Next.js BFF, so
the BFF address must be listed in the API setting `TrustedProxies` (environment form
`TrustedProxies__KnownProxies__0=<address>`); otherwise all users share one bucket.
The BFF forwards the end-user address in `X-Forwarded-For` only when it runs behind
an ingress that appends the real client address and `TRUSTED_INGRESS=true` is set
(`frontend/.env.example`). Local development settings trust the loopback addresses.

## Demo workspace

A Development API start generates the "Parley Demo" organization on whatever database
`Database:ConnectionString` points to. The C# generator creates about 5,000 contacts with mixed
English and Ukrainian names, 24 months of orders, companies, deals, tasks, notes, tags, two teams
and the demo accounts, then the RFM snapshot, the priority list and (when the scoring service
answers) the uplift and CLV scores with ML mode. It runs in the background after the migrations,
so `/health` answers while it works. The seed is deterministic for a fixed `RandomSeed` and
reference instant.

Enable it in the git-ignored `appsettings.Development.json`; the committed configuration keeps it
off, and a host that is not Development refuses to seed even when the switch is on:

```json
"Demo": {
  "Seed": {
    "Enabled": true,
    "AccountPassword": "test1234",
    "RefreshAfterDays": 7
  }
}
```

Every demo account (`test@gmail.com`, `admin`, `marketer`, `viewer`, two managers and six reps on
`demo.parley.test`) signs in with `Demo:Seed:AccountPassword`; no password file is written. A
workspace older than `RefreshAfterDays` is wiped and generated again on the next start, and a
failed start leaves the marker unchanged so the next start retries. To force a refresh sooner,
stop the API, delete the `identity.demo_seed_states` row of the demo organization, and start
again. The volumes and shares are defaults on `CrmDemoProfile`; a small profile through
`Demo:Seed:Profile` is useful for tests.

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

## Telegram bot

Users can receive their notifications in a Telegram chat. The channel is
off until a bot token is configured; without one the API runs normally and
`GET /telegram/status` reports `enabled: false`.

1. In Telegram, open `@BotFather`, send `/newbot`, and follow the prompts.
   Keep the token it returns and the bot's username (without the `@`).
2. Set the secrets from the environment, never in a committed file:
   `Telegram__BotToken` (the token) and `Telegram__BotUsername` (the
   username). Also set `Notifications__PublicWebBaseUrl` to the web app
   address, because the messages link to it.
3. Choose how updates arrive with `Telegram__Mode`:
   - `Polling` (default): the API asks Telegram for updates. It works on a
     local machine with no public address.
   - `Webhook`: Telegram posts updates to `POST /telegram/webhook`. Set
     `Telegram__WebhookSecret` to a long random value, register the address
     with the Bot API `setWebhook` call, and pass the same value as
     `secret_token`. Telegram then sends it in the
     `X-Telegram-Bot-Api-Secret-Token` header, and requests without it are
     refused.
4. Link a chat: sign in to the web app, create a link code, and open the
   returned deep link (`https://t.me/<bot>?start=<code>`). The code works once
   and expires after ten minutes. Then turn the Telegram switch on for the
   notification types you want in the notification preferences.

Local test: start the API in polling mode with a test bot token, create a
code in the app, send `/start <code>` to your bot, and send `/today` to see
the priority list. Use `/unlink` to end the link. Tests never call Telegram:
they use a fake bot client.

| Environment variable | Configuration key | Purpose |
| --- | --- | --- |
| `Telegram__BotToken` | `Telegram:BotToken` | Bot token; never logged or returned |
| `Telegram__BotUsername` | `Telegram:BotUsername` | Username for the deep link |
| `Telegram__WebhookSecret` | `Telegram:WebhookSecret` | Secret of the webhook header |
| `Telegram__Mode` | `Telegram:Mode` | `Polling` or `Webhook` |
| `Notifications__PublicWebBaseUrl` | `Notifications:PublicWebBaseUrl` | Web app address for message links |

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

### Scoring service

The API scores contacts through the optional FastAPI service in the compose profile `ml`. The service reads
artifacts from `ml/artifacts/models` (git-ignored) and requires a shared key:

```powershell
# .env: PARLEY_ML_KEY=<long random value, for example from openssl rand -hex 32>
docker compose --profile ml up -d
$env:Insights__Scoring__BaseUrl = "http://127.0.0.1:8090/"
$env:Insights__Scoring__ApiKey = $env:PARLEY_ML_KEY
dotnet run --project backend/src/Parley.Api
```

`PARLEY_ML_KEY` and `Insights:Scoring:ApiKey` must hold the same value; the key is never committed. The API validates
`Insights:Scoring:BaseUrl` at start, so a missing address stops it. Without an API key the API runs, but every scoring
call and model sync fails with a stable code.

Write the stub when no trained artifact exists (provenance `stub`, pipeline validation only, never an effect estimate):

```powershell
uv run --directory ml python scripts/write_stub_artifacts.py
```

Train the semi-synthetic demo `crm_uplift` model:

```powershell
uv run --directory ml --group train python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml
```

After adding or removing artifacts, sync the registry with `POST /insights/models/sync` as a user holding
`insights.ml.manage`, then choose the active version in ML settings. A version the last successful sync no longer reports
is marked unavailable: it stays as history and cannot be activated.

### Campaign sending

Campaign email needs these settings (`Campaigns:Send`); the API validates them at start:

| Environment variable | Purpose |
| --- | --- |
| `Campaigns__Send__UnsubscribeTokenKey` | HMAC key of the unsubscribe links, at least 32 characters |
| `Campaigns__Send__PublicApiBaseUrl` | Absolute API address for the one-click unsubscribe POST |
| `Campaigns__Send__PublicWebBaseUrl` | Absolute web address for the unsubscribe page |
| `Campaigns__Send__MessageIdDomain` | Domain of the deterministic `Message-ID` of each mail |
