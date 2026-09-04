# QuantWise — An AI-Powered Decision-Support Stock Advisory Platform

QuantWise delivers **personalised, risk-graded BUY / SELL / HOLD recommendations** to non-expert retail investors — without requiring any financial expertise. A gradient-boosted **cross-sectional ranking model** scores the investable universe against itself, FinBERT and analyst data supply a sentiment cross-check, and a deterministic risk-grading engine grades the result. A *constrained* Large Language Model (Google Gemini) then turns those pre-validated signals into plain-language, risk-tailored advice — it never forecasts, ranks, or invents a number.

> Graduation Project · 2025 / 2026 · Faculty of Computer Science, MSA University
> Aligned with UN Sustainable Development Goals **9** (Industry, Innovation & Infrastructure) and **10** (Reduced Inequalities).

---

## System Overview

QuantWise spans three tiers, all in this repository:

| Tier | Tech | Responsibility |
|------|------|----------------|
| **Frontend** | React 18 · TypeScript · Vite · TanStack Query · Framer Motion | "Quant Terminal" dashboard — onboarding, recommendations, portfolio, market hub, notifications |
| **Backend** | .NET 10 · ASP.NET Core · MediatR (CQRS) · EF Core · MassTransit | Modular monolith — auth, portfolios, recommendation orchestration + Gemini, notifications |
| **AI Pipeline** | Python · FastAPI · XGBoost · scikit-learn · FinBERT | Daily market-wide scoring: ranking → sentiment → risk grading |

Supporting infrastructure: **PostgreSQL 18**, **Redis 8** (HybridCache), **RabbitMQ 4** (Outbox/Inbox via MassTransit), JWT bearer auth.

### How a recommendation is produced

```
yFinance / Finnhub ──▶ FastAPI pipeline (XGBoost ranking + calibration + FinBERT + risk rules)
                              │  POST /api/score  (daily, triggered by .NET Quartz job)
                              ▼
        .NET Recommendations module ──▶ Gemini (constrained, schema-JSON)
                              │           personalises per user risk profile
                              ▼
        Redis cache (24h) ──▶ React dashboard  (BUY / SELL / HOLD + reason)
```

The LLM only ever sees **pre-graded, validated signals** — it never forecasts or invents prices, tickers, or numbers.

---

## Backend Modules

A modular monolith; each module owns its own `DbContext` and schema.

| Module | Responsibility |
|--------|----------------|
| **Users** | Registration, JWT login (BCrypt), profile |
| **Portfolio** | Risk questionnaire → Conservative / Moderate / Aggressive, target allocation |
| **Recommendations** | Ingests daily pipeline runs, calls Gemini, serves personalised picks (cached) |
| **Notifications** | Welcome & daily-run alerts, unread badges (consumes domain events over RabbitMQ) |

---

## Quick Start

### Prerequisites

- .NET 10 SDK
- Node.js 18+
- Docker & Docker Compose
- Python 3.11+ (only if running the pipeline outside Docker)

### 1. Start infrastructure + AI pipeline

```bash
docker-compose up -d
```

This brings up the FastAPI pipeline plus PostgreSQL, Redis, RabbitMQ, Mailpit, and pgAdmin. Provide `Pipeline/.env` (API keys, e.g. Finnhub) before starting.

| Service | URL / Port |
|---------|-----------|
| AI Pipeline (FastAPI) | http://localhost:8000 (`/health`, `POST /api/score`) |
| PostgreSQL | `localhost:5432` (postgres / postgres) |
| Redis | `localhost:6379` |
| RabbitMQ management | http://localhost:15672 (admin / admin) |
| Mailpit (email UI) | http://localhost:8080 |
| pgAdmin | http://localhost:5151 (admin@admin.com / admin) |

### 2. Apply database migrations

```bash
cd Backend/src/API/Project.Api
dotnet ef database update --context UsersDbContext
dotnet ef database update --context PortfolioDbContext
dotnet ef database update --context RecommendationsDbContext
dotnet ef database update --context NotificationsDbContext
```

### 3. Run the backend

```bash
cd Backend/src/API/Project.Api
dotnet run
```

API: http://localhost:5000 (https://localhost:7252). The backend's Quartz `FetchDailyPipelineJob` calls the pipeline's `POST /api/score` once per day; the Gemini API key is configured in the Recommendations module options.

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

App: http://localhost:3000 → talks to the backend on :5000.

---

## AI Pipeline

The pipeline (`Pipeline/`) is a single FastAPI service that scores the full ticker universe daily.

- **Phase 1 — Data acquisition**: ~100 US large-caps via yFinance; analyst ratings & news headlines via Finnhub.
- **Phase 2 — Feature engineering**: 14 technical indicators computed by one shared module (`core/features.py`) used by both training and serving, with `shift(1)` on every moving average so no feature can see its own bar. The champion consumes them raw; the hybrid rollback additionally MinMax-scales them and builds 60-day look-back windows.
- **Phase 3 — Ranking model**: XGBoost scores each name's expected 21-trading-day return *relative to the universe median*, and an isotonic calibrator turns that score into a real probability of beating the median. Serving is flag-controlled (`SERVING_MODEL`); the legacy LSTM→XGBoost hybrid stays available as a rollback.
- **Phase 4 — Risk grading**: deterministic `risk_rules.py` → agreement, risk level (LOW/MED/HIGH), conviction.

Endpoints: `GET /health`, `POST /api/score`, `POST /api/closes`, `POST /api/instrument-stats`, `POST /api/reproduce` (replays a stored feature snapshot for audit). Champion artefacts live in `Pipeline/models/ranking_v1/`; the hybrid rollback artefacts sit alongside in `Pipeline/models/`.

**Results** (held-out 2024-12-31 — 2026-06-09 slice, straight from `models/ranking_v1/metrics.json`):

| Metric | Champion | Reference |
|---|---|---|
| Information coefficient (mean daily Spearman) | **0.077** (t ≈ 9.2) | 0.064 for a naive Momentum_21 baseline |
| Directional hit rate | **51.5%** | 49.7% base rate (50% by construction) |
| Calibration error (ECE) | **3.8pp** | 11.5pp uncalibrated |

The edge is small and honestly reported: this ranks stocks against each other, it does not forecast prices. An absolute-return target was tried first and abandoned — 30-day returns are mostly market noise, and subtracting the per-date universe median cancels the part no model can predict. The LSTM was dropped on the same evidence: on the ranking target it *lost* 0.025 IC against trees alone (`models/ranking_v1/lstm_experiment.json`).

A cost-aware walk-forward backtest (`models/ranking_v1/backtest.json`, 25bps per side) is also published, but note its own caveats: the universe is survivorship-biased, and while the strategy beats the S&P 500 on return it trails an equal-weight basket of the same universe on Sharpe.

---

## Testing

- **Unit** — xUnit · NSubstitute (CQRS handlers, domain rules)
- **API / Integration** — xUnit + WebApplicationFactory over real Postgres & Redis (Testcontainers)
- **GUI / System** — Playwright (end-to-end, 12 black-box cases)
- **Load** — k6 (50 VUs, read paths)
- **Model** — information coefficient · hit rate vs the 50% base rate · decile spread · calibration error, all against a naive momentum baseline

68 automated tests across the suite. Quick manual check — log in, open the browser console, and run:

```javascript
window.triggerTestNotification()
```

---

## Project Structure

```
├── Backend/
│   └── src/
│       ├── API/Project.Api/   # ASP.NET Core entry point (Quartz daily job)
│       ├── Common/            # Shared infrastructure (Outbox/Inbox, messaging)
│       └── Modules/
│           ├── Users/         # Auth & profiles
│           ├── Portfolio/     # Risk profiling & allocation
│           ├── Recommendations/# Pipeline ingest + Gemini personalisation
│           └── Notifications/ # Notifications & emails
├── Pipeline/                  # FastAPI ML service (XGBoost ranking + FinBERT)
│   ├── main.py
│   ├── risk_rules.py
│   └── models/                # Champion (ranking_v1/) + hybrid rollback artefacts + registry.json
├── frontend/                  # React + TypeScript + Vite
│   └── src/{components,context,pages,services}
├── presentation/             # Slidev defence deck + exported PDFs
└── docker-compose.yml
```

---

## Documentation

- [System Architecture](SYSTEM_ARCHITECTURE.md)
- [Database ERD](DATABASE_ERD.md)
- [UML Diagrams](UML_DIAGRAMS.md)
- [Use-Case Diagram](USE_CASE_DIAGRAM.md)
- [Sequence Diagrams](SEQUENCE_DIAGRAM.md)
- [Running Guide](RUNNING.md)
- [Frontend Config](FRONTEND_CONFIG.md)

---

## Team

| Name | ID | Role |
|------|-----|------|
| Seif ElDein Mostafa | 235057 | Software Engineering |
| Yahia Ahmed | 235161 | Software Engineering |

**Supervision:** Dr. Marwa Solayman · Eng. Farah Darwish (TA)

**Publication:** *A Hybrid LSTM–XGBoost Framework for Multi-Horizon Stock Return Prediction Across Diversified Equity Portfolios* — accepted (pending publication), IEEE. The paper documents the earlier absolute-return hybrid; the system has since moved to the cross-sectional ranking model described above, and the reasoning for that change is recorded in `IMPLEMENTATION_PLAN.md` §§ 1.1–1.2.
