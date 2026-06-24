# QuantWise — Project Handoff Document

> AI-powered, decision-support stock advisory platform. Three-tier polyglot system:
> Python ML pipeline → .NET modular-monolith backend → React SPA. Delivers daily,
> personalised, risk-graded BUY/SELL/HOLD recommendations for the top ~100 US large-caps.
> The LLM is a constrained *synthesiser* over pre-computed, risk-graded signals — it never
> forecasts on its own.

---

## 1. Folder / File Structure

Top level:

```
Graduation-project/
├── Backend/                         # .NET 10 modular monolith
│   ├── Template.slnx
│   ├── global.json
│   ├── src/
│   │   ├── API/Project.Api/         # host: DI composition, middleware, Market proxy
│   │   │   ├── Market/              # FinnhubClient.cs, MarketEndpoints.cs
│   │   │   ├── Middlewares/
│   │   │   └── Extensions/
│   │   ├── Common/                  # shared kernel (5 projects)
│   │   │   ├── Project.Common.Domain            # Entity base, domain-event abstractions
│   │   │   ├── Project.Common.Application        # CQRS messaging, caching, behaviors
│   │   │   ├── Project.Common.Infrastructure     # auth, cache, outbox/inbox, EF, OTel
│   │   │   └── Project.Common.Presentation        # IEndpoint, ApiResults
│   │   └── Modules/
│   │       ├── Users/
│   │       ├── Portfolio/
│   │       ├── Recommendations/
│   │       └── Notifications/
│   └── tests/Modules/               # 6 test projects (5 unit + 1 integration)
│       ├── Users/...Domain.Tests, ...Application.Tests, ...IntegrationTests
│       ├── Portfolio/...Application.Tests
│       ├── Recommendations/...Application.Tests
│       └── Notifications/...Application.Tests
├── Pipeline/                        # Python FastAPI ML service
│   ├── main.py                      # /api/score: fetch → LSTM+XGBoost → FinBERT → risk rules
│   ├── risk_rules.py                # deterministic risk grading
│   ├── models/                      # trained artefacts (see §4)
│   ├── Dockerfile
│   ├── requirements.txt
│   ├── .env / .env.example
│   └── last_score_output.json       # copy of the most recent scoring response
├── frontend/                        # React 18 + Vite 5 SPA
│   └── src/{app,pages,features,components,shared}
├── docker-compose.yml
├── .env / .env.example              # backend secrets (.env is gitignored)
└── dissertation-evidence/           # diagrams, screenshots, loadtest, uat (not app code)
```

Every backend **module** uses the same 6-project vertical-slice layout:

```
Modules/<Name>/
├── Project.Modules.<Name>.Domain            # entities, domain events (no outward deps)
├── Project.Modules.<Name>.Application        # CQRS command/query handlers (MediatR)
├── Project.Modules.<Name>.Infrastructure     # EF Core DbContext, repos, clients, outbox/inbox
├── Project.Modules.<Name>.Presentation       # minimal-API IEndpoint definitions
├── Project.Modules.<Name>.IntegrationEvents  # cross-module event contracts (published)
└── Project.Modules.<Name>.PublicApi          # cross-module query interface (published)
```

---

## 2. Tech Stack & Each Service's Role

| Service | Tech | Role | Port |
|---|---|---|---|
| **frontend** | React 18, Vite 5, TypeScript (partial), TanStack Query 5, react-router 6, Framer Motion, lucide-react | SPA client ("Quant Terminal" dark/amber UI): onboarding, dashboard, market, learning, portfolios, profile | 3000 (vite dev) |
| **backend API** | .NET 10, ASP.NET Core minimal APIs, MediatR (CQRS), FluentResults, EF Core, MassTransit, Quartz, BCrypt, JWT HS256 | Domain logic, auth, recommendations, daily-run orchestration, market proxy | 5000 |
| **pipeline** | Python 3.11, FastAPI, PyTorch, XGBoost, transformers (FinBERT), yfinance, Finnhub, requests | Daily ML scoring → risk-graded predictions | 8000 |
| **postgres** | PostgreSQL 18 | Relational store; one schema per module | 5432 |
| **redis** | Redis 8 | HybridCache for LLM recommendation payloads (24h TTL) | 6379 |
| **rabbitmq** | RabbitMQ 4 (management) | Broker for cross-module integration events (outbox/inbox via MassTransit) | 5672 / 15672 |
| **mailpit** | axllent/mailpit | Dev SMTP capture (welcome emails) | 25 (SMTP→1025) / 8080 (UI→8025) |
| **pgadmin** | dpage/pgadmin4 | Optional DB admin UI | 5151 |

External APIs: **Google Gemini** `gemini-2.5-flash` (request-time recommendation personalisation), **Finnhub** (analyst consensus, news, live quotes/search), **Yahoo Finance** via `yfinance` (OHLCV, price targets, recommendations).

Two operational clocks:
- **Nightly batch** — Quartz `FetchDailyPipelineJob` (cron `0 0 1 ? * TUE,WED,THU,FRI,SAT`, 01:00 UTC Tue–Sat) calls `POST pipeline:8000/api/score`, dispatches `IngestDailyRunCommand`, stored as a dated `DailyRun`; a domain event fans out notifications.
- **Live request** — dashboard load → backend checks Redis → on miss, grounds a Gemini call on the latest run + user risk profile + prior holdings → caches 24h → returns picks.

---

## 3. API Endpoints

Base URL `http://localhost:5000`. Authenticated routes need `Authorization: Bearer <JWT>` (HS256).

### Users / Auth
| Method | Route | Auth | Payload / Notes |
|---|---|---|---|
| POST | `/api/users/register` | no | `{ email, password, firstName, lastName }` → 201 `{ id }`; password BCrypt-hashed; duplicate email rejected |
| POST | `/api/users/login` | no | `{ email, password }` → 200 `{ accessToken }`; generic 401 on bad creds |
| GET | `/api/users/profile` | yes | current user `{ id, firstName, lastName, email, role }` |
| GET | `/api/users/{id}` | yes | user by id |
| GET | `/api/users` | yes | list users |

### Portfolio
| Method | Route | Auth | Payload / Notes |
|---|---|---|---|
| POST | `/api/portfolios` | yes | `{ primaryGoal, timeHorizon, riskTolerance:int, marketReaction, investmentExperience, stocksPercentage, bondsPercentage, etfsPercentage, cashPercentage, riskProfile:string, investmentAmount:decimal }` → 201 |
| GET | `/api/portfolios/me` | yes | current user's portfolio |
| GET | `/api/portfolios/{id}` | yes | by id (owner-checked → 403 for non-owner) |
| PUT | `/api/portfolios/{id}` | yes | update in place (questionnaire retake; no duplicate) |

### Recommendations
| Method | Route | Auth | Payload / Notes |
|---|---|---|---|
| GET | `/api/recommendations` | yes | personalised ranked BUY/SELL/HOLD picks; 24h per-user Redis cache; first call/day hits Gemini |
| GET | `/api/predictions` | yes | latest market-wide run, ordered by conviction (learning view; no date picker) |
| POST | `/api/internal/daily-results` | API key | header `X-Pipeline-Key`; body = pipeline `ScoreResponse` `{ generated_at, count, records[] }`; **idempotent on `generated_at`** |

### Notifications
| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/api/notifications` | yes | paginated `?page=1&pageSize=20` |
| GET | `/api/notifications/unread-count` | yes | |
| PUT | `/api/notifications/{id}/read` | yes | mark one read |
| PUT | `/api/notifications/read-all` | yes | mark all read |
| POST | `/api/notifications/test` | yes | dev/test helper |

### Market (proxy group `/api/market`, all authed)
| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/api/market/search?q=<text>` | yes | symbol search via Finnhub (key stays server-side) |
| GET | `/api/market/quote?symbol=<sym>` | yes | live quote; 404 if unavailable |

### Pipeline (`http://localhost:8000`)
| Method | Route | Notes |
|---|---|---|
| POST | `/api/score` | runs the full daily scoring; returns `ScoreResponse { generated_at, count, records[] }` |
| GET | `/health` | liveness: models / FinBERT / Finnhub status |

`ScoreResponse.records[]` (== backend `PredictionRecordDto`): `ticker, direction, change_pct, confidence, sentiment_score, signal, analyst_rating, rating_label, pt_upside_pct, news_score, agreement, risk_level, conviction_score, risk_flags[], rationale`.

---

## 4. ML Model Details

**Pipeline** (`Pipeline/main.py` + `risk_rules.py`), FastAPI, models loaded once at startup.

### Universe & data
- Universe: top 100 US large-caps via `yfinance` `EquityQuery` screener (region US, exchanges NMS/NYQ, intraday market cap > $10B), sorted by market cap; hardcoded fallback list if the screener looks wrong.
- Price data: 6-month daily OHLCV (`yfinance` batch download), throttled to avoid rate limits; min 120 raw rows per ticker.

### Prediction model (ensemble)
- **LSTM backbone** (PyTorch `nn.LSTM`): 60-day sliding window, **5 input price features**, returns final hidden state + a linear direction/magnitude head.
- **XGBoost head**: input = LSTM final hidden features concatenated with **14 scaled technical indicators** → normalised return `z`, denormalised via `target_stats` to a next-day % change.
- **Confidence**: MC-dropout style — `MC_SAMPLES = 30` passes (`MC_SEED = 1234`); spread → stability; blended with signal strength and data quality. *Note: dropout is 0 on a single-layer LSTM, so MC is effectively a no-op there and stability pins to 1.0.*
- Features (engineered in `_compute_features`): Return, Volume_Ratio, RSI(14), MACD/MACD_signal/MACD_hist, SMA_{5,10,15,30}_Ratio, EMA_9_Ratio, Volatility_20, Momentum_{10,21}, Volume_Change.
- Concurrency safety: `torch.backends.mkldnn.enabled = False` (oneDNN LSTM kernel is non-deterministic off the main thread) + a model lock.

### Sentiment (top 35 candidates by projected return)
- **FinBERT** (`ProsusAI/finbert`) classifies up to ~150 Finnhub headlines/ticker (Yahoo fallback), filtered for company relevance.
- Composite sentiment score, weighted: `consensus 0.40, news 0.25, price_target 0.20, actions 0.15` → label POSITIVE/NEGATIVE/NEUTRAL (`±0.15` thresholds).

### Deterministic risk grading (`risk_rules.py`)
- Cross-validates quant direction vs sentiment → `agreement` ∈ {CONFIRMED, CONTRADICT, NEUTRAL}.
- Flags: `signal_confirmed/contradiction, low_conviction, extreme_move, thin_coverage, stale_analyst, internal_conflict`.
- `risk_level`: HIGH (contradiction / internal conflict / extreme+low-conf); MEDIUM (low conviction / thin coverage / stale / neutral agreement); else LOW.
- `conviction_score` ∈ [0,1] = `0.5*conf + 0.3*min(|sentiment|,1) ± 0.2` (agreement), sorted desc.
- Thresholds: `LOW_CONVICTION_PCT 1.5, LOW_CONFIDENCE 0.30, EXTREME_PCT 12.0, MIN_RATINGS 5, MIN_NEWS 3, STALE_DAYS 60, COMPONENT_CONFLICT 0.20`; aborts the run if fewer than `MIN_RECORDS = 25` survive.

### Training setup
- Trained **offline once** (notebook `models/Hybrid_Model_v2.ipynb`); only artefacts are committed. The running service infers only, never trains.
- Model artefacts in `Pipeline/models/`: `lstm_backbone.pth`, `xgb_head.json`, `global_feature_scaler.pkl`, `global_tech_scaler.pkl`, `universal_config.json` (look_back, feature_cols, tech_cols, lstm_params), `target_stats.json` (mean/std for denormalisation).

### LLM personalisation (backend, not pipeline)
- `GeminiLlmClient` calls `gemini-2.5-flash` `generateContent` with a **JSON response schema** `{ summary, picks[] }` (action enum `["BUY","SELL","HOLD"]`), `temperature 0.3`, retries on 429/5xx + up to 3 parse retries.
- System prompt constrains the model to a *synthesiser* role: use only provided data, respect risk grading, never invent numbers.

---

## 5. Docker Compose Config

File: `docker-compose.yml`. Network `project-network` (bridge).

| Service | Image | Ports (host:container) | Volumes | Notes |
|---|---|---|---|---|
| pipeline | build `./Pipeline` → `quantwise-pipeline:latest` | 8000:8000 | `yf_cache_pipeline:/opt/yf-cache` | `env_file: ./Pipeline/.env`; `YF_CACHE_DIR=/opt/yf-cache`; `restart: unless-stopped` |
| postgres | postgres:18 | 5432:5432 | `postgres_data:/var/lib/postgresql` | `POSTGRES_USER=postgres`, `POSTGRES_PASSWORD=postgres` |
| redis | redis:8-alpine | 6379:6379 | `redis_data:/data` | `redis-server --appendonly yes` |
| mailpit | axllent/mailpit:latest | 25:1025, 8080:8025 | — | dev SMTP capture |
| pgadmin | dpage/pgadmin4:latest | 5151:80 | `pgadmin_data:/var/lib/pgadmin` | `admin@admin.com / admin`; `depends_on: postgres` |
| rabbitmq | rabbitmq:4-management-alpine | 5672:5672, 15672:15672 | bind `./rabbitmq_data:/var/lib/rabbitmq` | `RABBITMQ_DEFAULT_USER/PASS=admin` |

Named volumes: `postgres_data, redis_data, pgadmin_data, rabbitmq_data, yf_cache_pipeline` (rabbitmq actually uses a bind mount `./rabbitmq_data`).

> The **backend API and frontend are NOT in compose** — they run on the host (`dotnet run` on :5000 from repo root so DotNetEnv finds `.env`; `npm run dev` on :3000). Typical local bring-up: `docker compose up -d postgres redis rabbitmq mailpit` (+ `pipeline` when needed), then start backend and frontend on the host. DB schema persists in the `postgres_data` volume; no auto-migration in `Program.cs`.

---

## 6. n8n Workflow Structure

**There is no n8n workflow artifact in the repository.** n8n was the *original* orchestration design; it has been fully replaced by the FastAPI pipeline service (`Pipeline/main.py`). The only remaining traces are comments:
- `risk_rules.py` header: "Python port of `risk_core.js` … Ported 1-to-1 from Risk Node/risk_core.js" and a `MIN_RECORDS` guard "mirrors the MIN_RECORDS guard in `n8n_code_node.js`".
- `DailyRun.cs`: "Ingested from the n8n pipeline."
- Ingest endpoint description: "The n8n pipeline POSTs the risk-graded daily run here."

The **equivalent workflow** (now all inside `/api/score`, historically the n8n nodes) is:

```
Trigger (daily cron, now Quartz FetchDailyPipelineJob)
  → Fetch universe (top-100 screener)        [was: HTTP/Function node]
  → Fetch OHLCV (yfinance, throttled)         [was: HTTP node]
  → Predict node (LSTM + XGBoost)             [was: ML/Code node]
  → Sentiment node (FinBERT + Finnhub)        [was: HTTP + Code node]
  → Risk node (risk_core.js → risk_rules.py)  [was: Code node, MIN_RECORDS guard]
  → POST /api/internal/daily-results          [was: HTTP node → backend ingest]
```

If a presentation needs an "n8n diagram," reconstruct it from the stages above — but state plainly that the production implementation is the single FastAPI endpoint, not n8n.

---

## 7. Environment Variables (names only)

### Backend `.env` (repo root; loaded by DotNetEnv; `.NET Section__Key` form)
- `Authentication__Key`            (JWT HS256 signing key, ≥256-bit)
- `ConnectionStrings__Database`     (Npgsql connection string)
- `RabbitMQ__Password`
- `Recommendations__Ingest__ApiKey` (shared key for `X-Pipeline-Key`)
- `Recommendations__Llm__ApiKey`    (Gemini)
- `Finnhub__ApiKey`                 (market proxy)

*(appsettings.json ships these blanked; `.env` supplies real values and is gitignored — template is `.env.example`.)*

### Pipeline `Pipeline/.env`
- `FINNHUB_API_KEY`     (required for analyst/news; else yfinance-only fallback)
- `SENTIMENT_WORKERS`   (default 8)
- `YF_RETRIES`          (default 3)
- `YF_MIN_INTERVAL`     (default 0.3)
- `YF_PROXY`            (optional)
- `YF_CACHE_DIR`        (set by compose via the volume)

### Frontend
- `VITE_API_URL` (optional override; defaults to `http://localhost:5000`)

---

## 8. Database Schema / Models

PostgreSQL, **one schema per module**, snake_case tables, **no cross-schema foreign keys** (modules link by `user_id` value only).

### schema `users` → `users`
`id (uuid PK)`, `first_name`, `last_name`, `email (unique)`, `hashed_password`, `role (int: 0=User, 1=Admin)`, `created_at (timestamptz)`

### schema `Portfolio` → `portfolios`
`id (uuid PK)`, `user_id (uuid)`, `primary_goal`, `time_horizon`, `risk_tolerance (int)`, `market_reaction`, `investment_experience`, `stocks_percentage (int)`, `bonds_percentage (int)`, `etfs_percentage (int)`, `cash_percentage (int)`, `risk_profile (int: 0=Conservative,1=Moderate,2=Aggressive)`, `investment_amount (numeric)`, `created_at`, `updated_at (nullable)`

### schema `Recommendations`
- `daily_runs`: `id (uuid PK)`, `generated_at (timestamptz, indexed — idempotency key)`, `count (int)`, `created_at`
- `stock_predictions`: `id (uuid PK)`, `daily_run_id (uuid FK → daily_runs)`, `ticker`, `direction (UP/DOWN)`, `change_pct (double)`, `confidence (double)`, `sentiment_score (double)`, `signal (POSITIVE/NEUTRAL/NEGATIVE)`, `analyst_rating (double?)`, `rating_label (text?)`, `pt_upside_pct (double?)`, `news_score (double?)`, `agreement (CONFIRMED/CONTRADICT/NEUTRAL)`, `risk_level (LOW/MEDIUM/HIGH)`, `conviction_score (double)`, `risk_flags (text[])`, `rationale (text)` — **1 run : many predictions** (only true parent-child FK in the model)
- `user_holdings`: `id (uuid PK)`, `user_id (uuid)`, `ticker`, `allocation_pct (double)`, `run_generated_at (timestamptz)`, `updated_at`

### schema `notifications` → `notifications`
`id (uuid PK)`, `user_id (uuid)`, `title`, `message`, `type (int: 0=Info,1=Warning,2=Success)`, `is_read (bool)`, `created_at`

### Per-schema messaging tables
Each module schema also carries `outbox_messages`, `inbox_messages` (+ consumer/state tables) for the transactional outbox/inbox pattern. Redis holds the recommendation cache keyed `recommendations:{userId}:{run yyyyMMddHHmmss}` (no authoritative state).

---

## 9. Known Issues, TODOs & Incomplete Parts

**Open / known:**
- **Marketing overclaim (BUG-07, open):** landing & onboarding copy advertises features that don't exist — "bank-level security / 256-bit encryption / 2FA / SOC 2", "automatic rebalancing", "continuous monitoring / optimisation", "real-time performance tracking". Also dead Google/Facebook social-login buttons on signup/login. Trim before any real launch.
- **Ingest has no minimum-record guard:** `POST /api/internal/daily-results` accepts any payload ≥1 record. The pipeline enforces `MIN_RECORDS=25`, but a manual POST bypasses it, so a tiny run can shadow real data (observed: a 2-record AAPL/MSFT run hid the 100-ticker run; the Learning page then showed only 2 stocks because `/api/predictions` returns the latest run by `generated_at`). Consider a guard on the endpoint.
- **Open CORS in dev**; TLS termination and a penetration test are pending (NFR-05 partial).
- **Onboarding risk calc is client-side** — the grade could be tampered with before reaching the server (move server-side).
- **Coverage is mid-range:** overall 47.3% line / 5.5% branch; core (excl. migrations/generated) 54.8% line / 64.6% method / 29.6% branch. Branch coverage is the weak spot.
- **No horizontal scaling / orchestration** — single server, single DB.
- **MC-dropout is a no-op** on the single-layer LSTM (dropout=0), so the confidence "stability" term is fixed at 1.0.
- **Welcome EMAIL template** still says generic "Welcome to Our Platform" (the in-app notification was fixed to "Welcome to QuantWise"; the email was not).
- **UAT/SUS study pending** — framework + participant handout exist (`dissertation-evidence/uat/`); needs ~10 real participants; result tables are placeholders.
- **FR-24 (recommendation export)** = explicitly out of scope this iteration (MoSCoW "Won't").

**Resolved (for context):** UTC ingest-timestamp 500 (BUG-01), portfolio/user IDOR owner check (BUG-02), `IUsersApi` DI crash (BUG-03), secrets-in-repo → `.env` + rotated JWT key (BUG-04), signup not auto-logging-in (BUG-05), stale welcome branding in notification (BUG-06).

---

## 10. Frontend Routes & Key Components

React 18 + Vite, react-router 6, route-level code-splitting (`React.lazy`). Server state via TanStack Query.

### Routes (`frontend/src/App.jsx`)
| Path | Component | Access |
|---|---|---|
| `/` | `pages/LandingPage` | public |
| `/login` | `pages/Auth/Login` | public |
| `/signup` | `pages/Auth/Signup` | public (auto-login → onboarding) |
| `/onboarding` | `pages/Onboarding` | private, full-screen (no shell) |
| `/dashboard` | `pages/Dashboard` | private, in `AppShell` |
| `/portfolios` | `pages/Portfolios` | private, in `AppShell` |
| `/simulator` | `pages/Simulator` (Learning Environment) | private, in `AppShell` |
| `/market` | `pages/Market` | private, in `AppShell` |
| `/profile` | `pages/Profile` | private, in `AppShell` |

### Key components & layers
- `app/AppShell.tsx` — shared layout: nav, notification bell, account menu (wraps the authed routes).
- `components/PrivateRoute.jsx` — JWT gate; redirects unauthenticated users.
- `features/recommendations/RecommendationsPanel.tsx` — dashboard picks (BUY/SELL/HOLD pills, allocation bars, dollar amounts).
- `features/recommendations/TargetMix.tsx` — portfolio target-mix view.
- `features/notifications/NotificationBell.tsx` — bell + unread count + list.
- `features/learning/usePredictions.ts` + `predictionsApi.ts` — `GET /api/predictions` for the Simulator.
- `shared/api/client` — `apiCall()` typed fetch wrapper (`requireAuth`, base URL, 404→empty handling).
- `shared/ui` — `LoadingState`, `EmptyState`, `ErrorState` (every async view renders one).
- Design system: "Quant Terminal" — dark ink `#0B0E11`, amber `#FFB000`; signal colours BUY=green, HOLD=amber, SELL=red; all in CSS design tokens.
- Landing sections under `components/` (Hero, Features, HowItWorks, Pricing, About, Footer, Navbar).

### Notable frontend behaviours
- Onboarding risk score + allocation computed **client-side**, then persisted via `POST /api/portfolios`.
- New-user dashboard probes `GET /api/portfolios/me` and gracefully handles 404 (no portfolio yet).
- Market search degrades to "Quote unavailable" for unresolved/delisted symbols.
- Simulator seeds its default asset mix from the **top 2** predictions by conviction; the dropdown lists the full latest-run set.

---

## Quick Start (local)

```bash
# 1. infra
docker compose up -d postgres redis rabbitmq mailpit
# 2. (optional) ML pipeline — heavy build (torch + FinBERT)
docker compose up -d --build pipeline       # :8000/health → models+finbert loaded
# 3. backend (from repo root so DotNetEnv finds .env)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Backend/src/API/Project.Api   # :5000/health
# 4. frontend
cd frontend && npm run dev                   # :3000
```

To populate recommendations without waiting for the cron: trigger `POST :8000/api/score`,
then POST the raw response to `:5000/api/internal/daily-results` with the `X-Pipeline-Key`
header (value = `Recommendations__Ingest__ApiKey` from `.env`).
