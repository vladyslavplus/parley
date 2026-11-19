# Parley

Parley is a multi-tenant customer relationship management (CRM) system built as a modular monolith: an ASP.NET Core
API with one PostgreSQL schema per module, a Next.js frontend that acts as a backend-for-frontend, and an optional
FastAPI service for uplift and customer lifetime value scoring. Campaigns are sent with a randomized holdout, so the
product reports incremental lift instead of raw response, and the priority list falls back to RFM when ML is off.

- `backend/` — .NET 10 solution: `Parley.Api`, five modules (Identity, Crm, Campaigns, Insights, Notifications), each
  with a `Contracts` project, and the shared kernel. Tests in `backend/tests`.
- `frontend/` — Next.js 16 App Router, React 19, TypeScript, Tailwind v4, next-intl (uk/en), Vitest and Playwright.
- `ml/` — Python 3.12 training, evaluation and the FastAPI scoring service; data cards and committed results.
- `docs/diagrams/` — architecture, sequence and state diagrams (Mermaid, rendered by GitHub).
- `scripts/demo-up.ps1` — one command from a clone to a seeded demo.

## Prerequisites

- .NET 10 SDK (10.0.401) and Docker Desktop with Compose
- Node 24 LTS with pnpm 12
- PowerShell 7 (`pwsh`) for the demo script
- `uv` for the ML module (optional for the CRM; without it the demo runs in RFM mode)

## Demo path

`scripts/demo-up.ps1` is the shortest path from a clone to the demo. It creates `.env` from `.env.example` when it is
missing, generates every value that is still a placeholder (database credentials, signing keys, the scoring key and
the demo password), starts PostgreSQL, Mailpit, the API, the web app and the scoring service in the compose profiles
`app` and `ml`, waits for the Development demo seed, and prints the addresses and demo accounts.

```powershell
pwsh scripts/demo-up.ps1
```

What it does:

- Checks Docker and Compose, and never overwrites a value that is already set in `.env`.
- Writes the scoring stub artifacts with `uv` when no uplift artifact exists. Without `uv` or a trained model the seed
  continues in RFM mode and says so.
- Runs the stack in the Development demo mode for that call only: `ASPNETCORE_ENVIRONMENT=Development`,
  `Demo__Seed__Enabled=true`, migrations on start. Nothing else switches the seed on.
- Waits for the API health endpoint, the web app and the demo marker (`identity.demo_seed_states`) with a 20-minute
  timeout, and reports progress while the workspace is generated.
- Is idempotent: a second run reuses the images, skips a workspace generated in the last 7 days and prints the same
  addresses.

Default addresses: web `http://127.0.0.1:3000/en/app`, API health `http://127.0.0.1:5000/health`, Mailpit
`http://127.0.0.1:8025`. The ports come from `.env` (`WEB_PORT`, `API_PORT`, `MAILPIT_HTTP_PORT`, ...), and only loopback
addresses are published. The Aspire dashboard and PostgreSQL stay on the ports of the git-ignored
`docker-compose.override.yml` (see below).

The plain `docker compose --profile app --profile ml up -d` is the production-like configuration: the API runs in
`Production`, never seeds, and a one-shot `migrate` service applies the migrations (`dotnet Parley.Api.dll --migrate`)
before the API starts. It needs a `.env` with real values; `scripts/demo-up.ps1` generates them when they are missing.

A second isolated stack (another project name, shifted ports and its own volumes) uses the same script:

```powershell
pwsh scripts/demo-up.ps1 -Project parley-verify -EnvFile "$env:TEMP\parley-verify.env" -ComposeFile docker-compose.yml
```

`-ComposeFile docker-compose.yml` skips the local override, so the verification stack cannot touch the local containers
or ports; put the shifted `WEB_PORT`/`API_PORT`/`MAILPIT_*` values in that env file.

## Local setup

Committed configuration holds placeholders only (`<db_host>`, `<auth_signing_key>` and so on). The API refuses to start
until every placeholder is replaced. Local values live in git-ignored files that the tracked examples describe.

1. Create the Compose environment file, or let `scripts/demo-up.ps1` create and fill it:

   ```powershell
   Copy-Item .env.example .env
   ```

   `POSTGRES_*`, `AUTH_SIGNING_KEY`, `UNSUBSCRIBE_TOKEN_KEY`, `PARLEY_ML_KEY` and the demo password have no usable
   committed value; `scripts/demo-up.ps1` generates them, and the API refuses to start while one is a placeholder.

2. Create the local port overrides. They publish PostgreSQL, the Aspire dashboard and the scoring service on
   `127.0.0.1` only (Mailpit and the `app` profile publish their ports already):

   ```powershell
   Copy-Item docker-compose.override.example.yml docker-compose.override.yml
   ```

3. Create the API development settings and replace each placeholder with the local value (the database user and
   password must match `.env`):

   ```powershell
   Copy-Item backend/src/Parley.Api/appsettings.Development.example.json backend/src/Parley.Api/appsettings.Development.json
   ```

4. Start the containers:

   ```powershell
   docker compose up -d
   ```

With the override in place, PostgreSQL with vector search is on `127.0.0.1:5433`, Mailpit is on `127.0.0.1:8025`
(SMTP `127.0.0.1:1025`), and the Aspire dashboard is on `127.0.0.1:18888` (OpenTelemetry on `127.0.0.1:4317`).
HybridCache keeps its entries in process memory; the API has no Redis client or connection setting.

## Run the API

```powershell
dotnet run --project backend/src/Parley.Api
```

The API listens on `http://localhost:5000`. The health endpoint is `GET /health`, the OpenAPI document is
`GET /openapi/v1.json`, and the Scalar reference is at `http://localhost:5000/scalar/v1` in Development.

Settings resolve in this order: `appsettings.json` (placeholders), then `appsettings.Development.json` in the
Development environment, then environment variables such as `Auth__SigningKey` (the highest priority). `dotnet run`
reads these files from the build output folder, so edits to `appsettings.Development.json` need a rebuild.

## Demo workspace

A Development API start generates the "Parley Demo" organization on whatever database `Database:ConnectionString`
points to. The C# generator creates about 5,000 contacts with mixed English and Ukrainian names, 24 months of orders,
companies, deals, tasks, notes, tags, two teams, the demo accounts and two historic campaigns with their results, then
the RFM snapshot, the priority list and (when the scoring service answers) the uplift and CLV scores with ML mode. It
runs in the background after the migrations, so `/health` answers while it works. The seed is deterministic for a fixed
`RandomSeed` and reference instant.

The two historic campaigns show the two outcomes the product has to report. `Demo: A/A, no effect` is a true
no-effect control: with the seed generated on 2026-11-13 it reports -0.65 percentage points (95 % CI -5.12 to +3.63,
p 0.772), so its interval covers zero. `Demo: simulated effect` sends to a 20 % holdout and applies the documented demo
effect of the model card times two to its treatment arm, which puts the lift above the minimum detectable effect of the
seeded audience: +8.27 percentage points (95 % CI +5.32 to +11.06, p < 0.00001, MDE 4.00), a measurable lift. Every
order the simulated campaign adds is labeled simulated wherever it appears. The seed dates both campaigns back from the
latest generated order, so a workspace generated on another date carries different calendar dates and the figures vary
with the seed date; the numbers above are a snapshot of the current seed, not constants.

Enable it in the git-ignored `appsettings.Development.json` for a host run; `scripts/demo-up.ps1` switches it on for the
containers. The committed configuration keeps it off, and a host that is not Development refuses to seed even when the
switch is on:

```json
"Demo": {
  "Seed": {
    "Enabled": true,
    "AccountPassword": "<demo_account_password>",
    "RefreshAfterDays": 7
  }
}
```

Every demo account (`test@gmail.com`, `admin@demo.parley.test`, `marketer@demo.parley.test`,
`viewer@demo.parley.test`, two managers and six reps on `demo.parley.test`) signs in with
`Demo:Seed:AccountPassword`; no password file is written. `scripts/demo-up.ps1` prints the password it generated. A
workspace older than `RefreshAfterDays` is wiped and generated again on the next start, and a failed start leaves the
marker unchanged so the next start retries. To force a refresh sooner, stop the API, delete the
`identity.demo_seed_states` row of the demo organization, and start again. The volumes and shares are defaults on
`CrmDemoProfile`; a small profile through `Demo:Seed:Profile` is useful for tests.

## Build and test

```powershell
dotnet build backend/Parley.slnx -warnaserror
dotnet format backend/Parley.slnx --no-restore --verify-no-changes
dotnet test backend/tests/Parley.UnitTests
dotnet test backend/tests/Parley.IntegrationTests
pnpm --dir frontend run check
pnpm --dir frontend run test
pnpm --dir frontend run build
uv run --directory ml pytest
```

Integration tests start a disposable PostgreSQL container with the same image as the local stack, so Docker must be
running. They supply their own settings and do not read the git-ignored Development file, so they run on a clean clone.
The whole suite is budgeted at about ten minutes.

### Browser smoke test

The smoke test signs in as the marketer, checks the priority list, builds and sends a small campaign from the seeded
at-risk segment, asserts that the message reached Mailpit, then switches ML off to see RFM mode and restores the switch.
It needs a seeded stack and `en` locales, and it leaves the seeded campaigns untouched.

```powershell
$env:BASE_URL = "http://127.0.0.1:3000"
$env:DEMO_SEED_PASSWORD = "<the demo password printed by scripts/demo-up.ps1>"
$env:MAILPIT_URL = "http://127.0.0.1:8025"
pnpm --dir frontend exec playwright test e2e/smoke.spec.ts
```

`BASE_URL` defaults to `http://127.0.0.1:3000`. `PLAYWRIGHT_WEB_SERVER=true` starts `pnpm dev` for a UI-only run instead
of using an already running stack.

## ML

```powershell
uv run --directory ml pytest
docker build -f ml/Dockerfile.train -t parley-ml-train ml
docker run --rm --gpus all parley-ml-train python -m parley_ml.gpu_check
```

### Scoring service

The API scores contacts through the optional FastAPI service in the compose profile `ml`. The service reads artifacts
from `ml/artifacts/models` (git-ignored) and requires a shared key:

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

Write the stub when no trained artifact exists (provenance `stub`, pipeline validation only, never an effect estimate);
`scripts/demo-up.ps1` does this itself when `uv` is available:

```powershell
uv run --directory ml python scripts/write_stub_artifacts.py
```

After adding or removing artifacts, sync the registry with `POST /insights/models/sync` as a user holding
`insights.ml.manage`, then choose the active version in ML settings. A version the last successful sync no longer reports
is marked unavailable: it stays as history and cannot be activated.

### Reproduce the models and the research results

The semi-synthetic demo `crm_uplift` model (provenance `semi_synthetic`; covariates and baseline outcomes are real, the
effect is injected):

```powershell
uv run --directory ml --group train python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml
```

RQ1 (uplift on the Criteo research dataset; the test split is opened only by the recorded test phase):

```powershell
uv run --directory ml python -m parley_ml.uplift.train_grid --config configs/uplift_grid.yaml
uv run --directory ml python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml --verify-saved-models
uv run --directory ml python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml --render-only
uv run --directory ml python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml --compare-reproduction
```

RQ2 (BG/NBD and Gamma-Gamma CLV on the clean Online Retail II data):

```powershell
uv run --directory ml python -m parley_ml.datasets.online_retail --config configs/online_retail.yaml
uv run --directory ml python -m parley_ml.clv.fit --config configs/clv_fit.yaml
uv run --directory ml python -m parley_ml.clv.evaluate --config configs/clv_eval.yaml
```

Committed tables, curves and notes are in `ml/results/`; model artifacts and run folders are git-ignored. Data cards with
checksums, cleaning counts and the exact schema are in `ml/datacards/`.

## Dataset attribution and licenses

Parley uses two public datasets. They are never committed; see the data cards for source URLs, checksums and cleaning.

- **Online Retail II** by Daqing Chen, UCI Machine Learning Repository, 2012, DOI
  [10.24432/C5CG6D](https://doi.org/10.24432/C5CG6D), is licensed under
  [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Attribution: "Chen, Daqing. Online Retail II [Dataset]. UCI
  Machine Learning Repository, 2012. https://doi.org/10.24432/C5CG6D." The demo export manifest carries this attribution.
- **Criteo uplift research dataset v2.1** is licensed under
  [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/): non-commercial use only, share-alike. It is used
  for research only; its results and derived models must not be used commercially, and a model trained on it never serves
  real contacts. Citation: Diemert, Eustache; Betlei, Artem; Renaudin, Christophe; Amini, Massih-Reza. "A Large Scale
  Benchmark for Uplift Modeling." AdKDD 2018. arXiv:2111.10106.

The dependency rule is that every library must be free for non-commercial use and must not block commercial use later.
The backend uses MIT, Apache-2.0, BSD-3-Clause and PostgreSQL-licensed NuGet packages; the Hangfire packages are
LGPL-3.0, used unmodified as NuGet dependencies. The frontend uses mostly MIT and Apache-2.0 packages, with MPL-2.0
(`lightningcss`, `axe-core`), LGPL-3.0-or-later (the optional sharp image codec) and CC-BY-4.0 data (`caniuse-lite`).
The Python environment uses MIT, BSD and Apache-2.0 packages, with MPL-2.0 (`certifi`, `pathspec`, `tqdm`). The fonts
Onest, Source Serif 4 and JetBrains Mono are licensed under the SIL Open Font License 1.1. The full inventory, with
versions and the license of every package, is kept with the local defense evidence (not committed).

## Diagrams

- [System context](docs/diagrams/architecture-context.md) — people, the system and external services.
- [Containers](docs/diagrams/containers.md) — web BFF, API and workers, database, scoring service, Mailpit.
- [Module dependencies](docs/diagrams/module-dependencies.md) — module and `Contracts` reference graph, event flows.
- [Entity relationship diagrams](docs/diagrams/erd.md) — one diagram per schema plus an overview.
- [Sign-in and refresh](docs/diagrams/sign-in-refresh.md) — the BFF cookie and token-rotation flow, including reuse.
- [Send pipeline](docs/diagrams/send-pipeline.md) — freeze, holdout, send claim and attribution.
- [Scoring flow](docs/diagrams/scoring-flow.md) — a scoring run, its failure path and the RFM fallback.
- [Outbox delivery](docs/diagrams/outbox-delivery.md) — transactional outbox to an idempotent inbox.
- [Campaign and send states](docs/diagrams/campaign-states.md) — the two state machines.
- [Roles and permissions](docs/diagrams/roles-permissions.md) - the five roles and the full grant matrix.
- [Deployment](docs/diagrams/deployment.md) - compose profiles, ports, networks and startup order.
- [ML pipeline](docs/diagrams/ml-pipeline.md) - data, split, training, artifact, registry and serving.
- [Telegram link and notifications](docs/diagrams/telegram-link.md) - one-time link code, commands and the delivery worker.

## Transactional email

Account emails (confirmation and password reset) are sent through `ITransactionalEmailSender` with MailKit SMTP and
Fluid templates in `uk` and `en`. The caller passes the recipient culture explicitly. Known cultures are `uk` and `en`;
any other value, including an empty value, falls back to `Email:DefaultCulture`.

Local development uses Mailpit. In the `Development` environment the API connects to `127.0.0.1:1025`, so
`docker compose up -d` is enough to send and inspect messages at `http://127.0.0.1:8025`.

Configuration is bound from `appsettings.json` and `appsettings.Development.json` and overridable by environment
variables:

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

Real SMTP credentials are never committed: set `Email__Username` and `Email__Password` from the deployment
environment. Send attempts log only the template key and culture, never the recipient address or body.

## Telegram bot

Users can receive their notifications in a Telegram chat. The channel is off until a bot token is configured; without
one the API runs normally and `GET /telegram/status` reports `enabled: false`.

1. In Telegram, open `@BotFather`, send `/newbot`, and follow the prompts. Keep the token it returns and the bot's
   username (without the `@`).
2. Set the secrets from the environment, never in a committed file: `Telegram__BotToken` (the token) and
   `Telegram__BotUsername` (the username). Also set `Notifications__PublicWebBaseUrl` to the web app address, because
   the messages link to it.
3. Choose how updates arrive with `Telegram__Mode`:
   - `Polling` (default): the API asks Telegram for updates. It works on a local machine with no public address.
   - `Webhook`: Telegram posts updates to `POST /telegram/webhook`. Set `Telegram__WebhookSecret` to a long random
     value, register the address with the Bot API `setWebhook` call, and pass the same value as `secret_token`.
     Telegram then sends it in the `X-Telegram-Bot-Api-Secret-Token` header, and requests without it are refused.
4. Link a chat: sign in to the web app, create a link code, and open the returned deep link
   (`https://t.me/<bot>?start=<code>`). The code works once and expires after ten minutes. Then turn the Telegram
   switch on for the notification types you want in the notification preferences.

Local test: start the API in polling mode with a test bot token, create a code in the app, send `/start <code>` to your
bot, and send `/today` to see the priority list. Use `/unlink` to end the link. Tests never call Telegram: they use a
fake bot client. `/today` shows actions and links, never contact names.

| Environment variable | Configuration key | Purpose |
| --- | --- | --- |
| `Telegram__BotToken` | `Telegram:BotToken` | Bot token; never logged or returned |
| `Telegram__BotUsername` | `Telegram:BotUsername` | Username for the deep link |
| `Telegram__WebhookSecret` | `Telegram:WebhookSecret` | Secret of the webhook header |
| `Telegram__Mode` | `Telegram:Mode` | `Polling` or `Webhook` |
| `Notifications__PublicWebBaseUrl` | `Notifications:PublicWebBaseUrl` | Web app address for message links |

## Campaign sending

Campaign email needs these settings (`Campaigns:Send`); the API validates them at start:

| Environment variable | Purpose |
| --- | --- |
| `Campaigns__Send__UnsubscribeTokenKey` | HMAC key of the unsubscribe links, at least 32 characters |
| `Campaigns__Send__PublicApiBaseUrl` | Absolute API address for the one-click unsubscribe POST |
| `Campaigns__Send__PublicWebBaseUrl` | Absolute web address for the unsubscribe page |
| `Campaigns__Send__MessageIdDomain` | Domain of the deterministic `Message-ID` of each mail |

## Rate limits behind the web BFF

The API rate-limits by client address. Browsers reach it through the Next.js BFF, so the BFF address must be listed in
the API setting `TrustedProxies` (environment form `TrustedProxies__KnownProxies__0=<address>`); otherwise all users share
one bucket. The BFF forwards the end-user address in `X-Forwarded-For` only when it runs behind an ingress that appends
the real client address and `TRUSTED_INGRESS=true` is set (`frontend/.env.example`). Local development settings trust the
loopback addresses.

## Limitations

- The demo `crm_uplift` model is semi-synthetic: its covariates and baseline outcomes are real, its treatment effect is
  injected. It validates the pipeline and the ranking code; it is not an estimate of a real email effect.
- The demo workspace is generated data. Contacts, names, companies, deals, tasks and notes are synthesized; orders and
  the retail data behind them come from Online Retail II. The historic A/A and simulated campaigns are fixtures.
- The Criteo research results validate the method on anonymized advertising features, not CRM fields or email; that
  model never ships.
- With a 10 % holdout in a small organization the study has low power; the product reports the minimum detectable effect
  instead of hiding it. A result that includes an injected order is labelled simulated everywhere.
- CLV does not affect the priority ranking today: the ML priority value is the additive uplift-based rule, not
  `uplift × CLV`. Value-weighted targeting remains a roadmap item.
- ML is optional. Without the scoring service the priority list stays in RFM mode and every CRM screen keeps working;
  a failed run leaves the last successful scores in place.
- There is no hosting, no published container images and no cloud deployment: the supported path is Docker Compose on
  one machine, with loopback-only published ports.
