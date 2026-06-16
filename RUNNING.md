# Running QuantWise locally

The stack is four parts, each in its own terminal. Run from the repo root unless noted.

| Service | URL | Notes |
|---------|-----|-------|
| Frontend (Vite/React) | http://localhost:3000 | |
| Backend (.NET API) | http://localhost:5000 | `/health` for status |
| Pipeline (FastAPI/ML) | http://localhost:8000 | `/health` shows model load state |
| Mailpit (email UI) | http://localhost:8080 | |
| RabbitMQ (UI) | http://localhost:15672 | admin / admin |

**Prerequisites:** .NET 10 SDK, Node.js, Docker Desktop, Python 3.11 with `Pipeline/requirements.txt` installed.

---

## 1. Infrastructure (Docker)

postgres (5432), redis (6379), rabbitmq (5672), mailpit (25/8080):

```powershell
docker compose up -d postgres redis rabbitmq mailpit
```

> The `pipeline` and `pgadmin` compose services are intentionally skipped — we run the pipeline directly (below).

---

## 2. Backend → http://localhost:5000  *(terminal 2)*

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project Backend/src/API/Project.Api
```

The Finnhub key (for the Market proxy) is read from the `Finnhub__ApiKey` environment variable — it is **not** stored in `appsettings.json`. A persistent user env var is set on the dev machine, so a fresh terminal picks it up automatically. If it doesn't, set it first:

```powershell
$env:Finnhub__ApiKey = "<your-finnhub-key>"
```

---

## 3. Frontend → http://localhost:3000  *(terminal 3)*

```powershell
cd frontend
npm install        # first run only
npm run dev
```

The frontend defaults its API base URL to `http://localhost:5000` (override with `VITE_API_URL` only if the backend port changes).

---

## 4. Pipeline → http://localhost:8000  *(terminal 4)*

The Python app does not auto-load `.env`, so load it into the process first, then start uvicorn:

```powershell
cd Pipeline
Get-Content .env | Where-Object { $_ -match '=' -and $_ -notmatch '^\s*#' } | ForEach-Object { $k,$v = $_ -split '=',2; [Environment]::SetEnvironmentVariable($k.Trim(), $v.Trim()) }
uvicorn main:app --host 0.0.0.0 --port 8000
```

First run downloads the FinBERT weights (~once). `GET /health` reports `models` / `finbert` as `loaded` when ready.

---

## Verify

```powershell
curl http://localhost:5000/health     # backend  -> 200
curl http://localhost:8000/health     # pipeline -> models/finbert "loaded"
# open http://localhost:3000 in a browser
```

Test login: `phase1test@quantwise.dev` / `Test1234!`

---

## First-time database setup (only on an empty DB)

The schema persists in the `postgres_data` Docker volume. On a fresh database, apply the four EF Core contexts:

```powershell
dotnet ef database update --project Backend/src/Modules/Users/Project.Modules.Users.Infrastructure --startup-project Backend/src/API/Project.Api --context UsersDbContext
dotnet ef database update --project Backend/src/Modules/Portfolio/Project.Modules.Portfolio.Infrastructure --startup-project Backend/src/API/Project.Api --context PortfolioDbContext
dotnet ef database update --project Backend/src/Modules/Notifications/Project.Modules.Notifications.Infrastructure --startup-project Backend/src/API/Project.Api --context NotificationsDbContext
dotnet ef database update --project Backend/src/Modules/Recommendations/Project.Modules.Recommendations.Infrastructure --startup-project Backend/src/API/Project.Api --context RecommendationsDbContext
```

(Requires `dotnet tool install --global dotnet-ef` if the `ef` tool isn't installed.)

---

## Predictions / daily run

The backend's Quartz job (`FetchDailyPipelineJob`) calls the pipeline's `POST /api/score` and ingests the result at **01:00 UTC, Tue–Sat**. To force a fresh run on demand while the pipeline is up:

```powershell
curl -X POST http://localhost:8000/api/score    # heavy: ~100-ticker predict + sentiment, takes minutes
```

…then it is ingested on the next cron tick (or wire an on-demand ingest).

---

## Stop

```powershell
# Ctrl+C in the backend / frontend / pipeline terminals, then:
docker compose down        # data persists in named volumes
```
