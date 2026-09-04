# Deployment & nightly-job reliability

The nightly jobs — instrument-price refresh (03:00 UTC), portfolio valuation
(03:30), **shadow model-portfolio run (03:45)**, digest, monitoring, outcome
scoring — are Quartz cron jobs hosted **inside the .NET backend process**. They
only fire while that process is alive. For the shadow track record to accrue a
continuous daily history, the backend must run reliably at those ticks.

## Run the whole stack always-on (recommended)

Everything is in `docker-compose.yml` with `restart: unless-stopped`, so Docker
restarts each service on crash or host reboot. Run it on an always-on host (a
small VPS, not a laptop, and **not** a free tier that sleeps on idle):

```bash
docker compose up -d --build
```

Services: `postgres`, `redis`, `rabbitmq` (health-checked), `pipeline`
(FastAPI :8000), and `backend` (.NET API on host :5099 → container :8080). The
backend waits for the infra healthchecks before starting (`depends_on:
condition: service_healthy`), so startup ordering is deterministic.

Secrets come from the repo-root `.env` (JWT key, Gemini/Finnhub keys, ingest
key); the compose `environment:` block overrides the host-oriented values with
in-network service DNS names (`postgres`, `redis`, `rabbitmq`, `pipeline`).

> The database schema must already be migrated (the app does not auto-migrate).
> For a fresh volume, run `dotnet ef database update` for each module context
> once, then `docker compose up -d`.

## Three layers of reliability

1. **Always-on host + auto-restart** (above) — the primary guarantee.
2. **Fire-and-proceed misfire** on every nightly cron trigger: a tick missed
   during a brief restart runs once on recovery instead of being skipped.
3. **Startup catch-up + missed-run alert** — belt and suspenders:
   - `ShadowCatchUpService` runs on boot: if it's past the tick window
     (04:15 UTC) and today has no snapshot, it fires the job once.
   - If the run executes but writes **zero** snapshots (stale prices, no
     published daily run), it raises a `ShadowRunBlockedIntegrationEvent` →
     every Admin user gets an ops notification. Silence never looks like success.

## Manual / external trigger

Run the shadow job on demand (idempotent per UTC day) — for testing, backfilling
a missed day, or driving it from an external scheduler (GitHub Actions cron,
cron-job.org) if you'd rather not rely on the in-process scheduler:

```bash
curl -X POST http://localhost:5099/api/internal/shadow/run \
  -H "X-Pipeline-Key: $INGEST_API_KEY"
```

Returns `202 Accepted`; watch `docker logs quantwise-backend` for the run summary.

## Notes

- Cron times are **UTC** — ensure the host clock is correct.
- Non-trading days (weekends) naturally no-op: prices are stale, so the job skips
  and revalues on the next trading day. That's expected, not an error.
- `ASPNETCORE_ENVIRONMENT=Development` in compose keeps console logging on so
  `docker logs` is useful; switch to `Production` once a log aggregator is wired.
