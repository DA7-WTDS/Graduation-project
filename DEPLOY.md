# Deploying QuantWise for a Demo (Free)

A single-instance, free showcase deployment:

- **Frontend** → Vercel (static Vite build)
- **Backend** → Render (Docker web service, free)
- **PostgreSQL** → Render free Postgres (holds pre-seeded data)
- **Redis / RabbitMQ** → not hosted — `DemoMode=true` runs both in-memory
- **AI pipeline** → not hosted — the dashboard reads yesterday's pre-seeded scoring run

> `DemoMode` only changes the hosted build. Local `docker-compose` and any real
> production deployment keep the real Redis + RabbitMQ.

---

## 1. Backend + Database on Render

The repo already contains [`render.yaml`](render.yaml) (Blueprint) and
[`Backend/Dockerfile`](Backend/Dockerfile).

### Option A — Blueprint (recommended)
1. Push this branch to GitHub.
2. Render Dashboard → **New → Blueprint** → pick this repo. Render reads
   `render.yaml` and creates **quantwise-db** (Postgres) and **quantwise-api** (web).
3. Fill the env vars marked `sync: false` (see step 3 below), then **Apply**.

### Option B — Manual
1. **New → PostgreSQL** (free plan) → name `quantwise-db`.
2. **New → Web Service** → this repo → **Runtime: Docker**,
   **Root Directory: `Backend`**, **Dockerfile Path: `Dockerfile`**,
   **Health Check Path: `/health`**, plan **Free**.
3. Add the env vars below.

---

## 2. Connection string (Npgsql format)

Render shows the DB as a `postgres://…` URL, but **.NET/Npgsql needs keyword
format**. Take the values from the Render DB page and build:

```
Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true
```

Use the **Internal** host for the `ConnectionStrings__Database` env var on the web
service (same-region, no egress). Use the **External** host when running migrations
from your laptop (step 4).

---

## 3. Environment variables (web service)

| Key | Value |
|-----|-------|
| `DemoMode` | `true` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Database` | *(Npgsql keyword string, internal host)* |
| `Authentication__Key` | *(long random secret — the JWT signing key)* |
| `Authentication__Authority` | `https://quantwise-api.onrender.com/` *(your service URL)* |
| `Recommendations__Llm__ApiKey` | *(Gemini key — only used on a cache miss)* |
| `Finnhub__ApiKey` | *(market-quotes key)* |
| `Recommendations__Ingest__ApiKey` | *(any value; pipeline isn't hosted)* |

`Authentication__Authority` is both the token issuer and the validation issuer — as
long as it's consistent it can be any URL; using the service URL is cleanest.

---

## 4. Apply migrations + seed data

The app does **not** auto-migrate. From your laptop, point EF at the **external**
connection string and run all four contexts:

```bash
cd Backend/src/API/Project.Api
$env:ConnectionStrings__Database = "<external Npgsql keyword string>"   # PowerShell
dotnet ef database update --context UsersDbContext
dotnet ef database update --context PortfolioDbContext
dotnet ef database update --context RecommendationsDbContext
dotnet ef database update --context NotificationsDbContext
```

**Seed the pre-computed scoring run** (so the dashboard has data without the
pipeline). Easiest is to copy your local demo data up with `pg_dump`:

```bash
# from local Postgres → Render Postgres (external URL)
pg_dump "<local conn>" --data-only --no-owner -t 'recommendations.*' \
  | psql "<render external conn>"
```

Adjust table/schema names to what your recommendations run actually populates
(the daily run + scored records). Alternatively, restore a full `pg_dump` of your
local DB if that's simpler.

---

## 5. Frontend on Vercel

The client reads the API base URL from `VITE_API_URL`
(`frontend/src/shared/api/client.ts`, defaulting to `http://localhost:5000`).

1. Vercel → **New Project** → this repo → **Root Directory: `frontend`**
   (framework auto-detects Vite).
2. **Environment Variable:** `VITE_API_URL = https://quantwise-api.onrender.com`
   (your Render URL, no trailing slash).
3. Deploy.

Backend CORS is already `AllowAll`, so no backend change is needed.

---

## 6. Before you present

- **Warm the backend.** The free Render service sleeps after ~15 min idle and
  cold-starts in ~50s. Open the site (or hit `/health`) a minute before demoing.
- **Pre-warm recommendations.** Log in as your demo user and open the dashboard
  once, so results are cached (a cache miss triggers a live Gemini call).
- **Fallback.** Keep the local `docker-compose` demo ready — it runs fully offline
  against pre-seeded data if venue Wi-Fi fails.

---

## Notes / caveats

- In-memory cache & message bus are **not durable** — a Render restart clears the
  cache (re-triggers a Gemini call on next view). Fine for a single-instance demo.
- The 1 AM `FetchDailyPipelineJob` still fires but fails gracefully (no pipeline
  reachable) and leaves your seeded data intact. To silence it, set
  `Recommendations__Pipeline__CronSchedule` to a non-firing expression.
- Render's free Postgres is time-limited — fine for an event, not for production.
