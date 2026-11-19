# Containers (C4 level 2)

```mermaid
flowchart TB
    browser["Browser<br/>React 19 client components"]

    subgraph system["Parley"]
        web["Web app and BFF<br/>Next.js 16, Node 24, port 3000<br/>httpOnly session cookies<br/>proxy.ts route guard<br/>/api/auth/* and /api/backend/*"]
        api["API and workers<br/>ASP.NET Core (.NET 10), port 8080<br/>controllers, Mediator handlers<br/>Hangfire schedules, outbox dispatchers<br/>migrations hosted services"]
        ml["Scoring service (optional, profile ml)<br/>FastAPI, port 8000<br/>/health, /models, /score/uplift, /score/clv"]
        db[("PostgreSQL 17 + pgvector<br/>schemas identity, crm, campaigns,<br/>insights, notifications, hangfire")]
        mailpit["Mailpit (profile-local mailbox)<br/>SMTP 1025, HTTP 8025"]
        aspire["Aspire dashboard (profile-local)<br/>OTLP 18889, UI 18888"]
    end

    telegram["Telegram Bot API"]
    smtp["Production SMTP server"]

    browser -->|"HTTPS, cookies only"| web
    web -->|"server-side HTTP, bearer token<br/>X-Forwarded-For when trusted"| api
    api -->|"Npgsql, one schema per module"| db
    api -->|"HTTP with X-Parley-Ml-Key<br/>batched scoring and model sync"| ml
    api -->|"SMTP"| mailpit
    api -->|"SMTP when configured instead of Mailpit"| smtp
    api -->|"OTLP traces, metrics, logs"| aspire
    api -->|"long polling or webhook"| telegram
```

Reading guide: the browser never talks to the .NET API directly. Next.js is a backend-for-frontend: it keeps the access and refresh tokens in httpOnly cookies, guards the application routes in `proxy.ts`, and forwards only allow-listed endpoints through `/api/backend/*` and the explicit `/api/auth/*` handlers. The API is a modular monolith in one container; the Hangfire worker, the outbox dispatchers and the migrations hosted services run inside the same process, which is why the diagram shows one API container rather than separate workers. PostgreSQL holds one schema per module plus Hangfire's own schema; no module reads another module's tables. The scoring service is an optional container: without it the API keeps working and the priority list stays in RFM mode. Mailpit and the Aspire dashboard are local-development conveniences; production replaces Mailpit with a real SMTP server and the dashboard stays operator-only. There is no Redis or Valkey service: HybridCache stays in process memory and the API has no Redis client or connection setting.

Verified against: `docker-compose.yml` (services, profiles, ports, the `ml-internal`/`ml-edge` networks), `frontend/src/proxy.ts` and `frontend/src/app/api/**` (BFF surface), `backend/src/Parley.Api/Program.cs` and `Startup/**` (workers in process), `backend/src/Modules/*/Module.cs` (one DbContext and schema per module).

