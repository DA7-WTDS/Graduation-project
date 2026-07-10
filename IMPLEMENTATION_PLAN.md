# QuantWise — Business Implementation Plan

**From graduation project to goal-based robo-advisory for Egyptian retail investors (EGX + US).**

The product: users answer a short questionnaire and receive a **customized, context-aware portfolio** matched to their goal — a set-and-forget retirement portfolio with crash monitoring, or an active high-risk portfolio with dip-buying and IPO/catalyst ideas. The AI decides deterministically from data; the LLM explains in plain language.

This plan supersedes the academic framing. The hybrid model is no longer a "contribution to defend" — every component must pay rent.

---

## Guiding decisions (already made in discussion)

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Strategy engine over stock picker.** Profile → strategy template → sleeves → allocation. ML is one input among several. | Portfolios are the product; picks are an ingredient. |
| D2 | **Relative ranking target**, not absolute return regression. | Absolute 30-day returns are ~mostly market noise (R² was negative). Ranking within a universe cancels market noise and is what portfolio construction actually needs. |
| D3 | **Context-awareness lives in data + rules, not in Gemini.** Instrument registry with metadata; deterministic selection; LLM narrates. | Reproducible, auditable, regulator-friendly; LLM kept where it's strong. |
| D4 | **Two model instances (EGX, US), one codebase.** Never rank across markets. | Different calendars (Sun–Thu vs Mon–Fri), liquidity, price limits, macro drivers, sentiment language. |
| D5 | **Cross-market split is an allocation decision**, made by the strategy engine (USD exposure = devaluation hedge for Egyptian users). | Currency risk is a top-3 concern for the target user. |
| D6 | **Deterministic allocation optimizer; Gemini explain-only.** | Allocations must be reproducible and auditable. |
| D7 | **Questionnaire redesigned around goals**, scored server-side, versioned, gates speculative access. | Single risk bucket cannot drive templates/sleeves/cadence; compliance audit trail. |
| D8 | **LSTM survives only if it earns its keep** (feature-importance test). | Its embedding added ~nothing over XGBoost on indicators; it doubles serving complexity. |
| D9 | **Daily batch stays.** No intraday/real-time. | Cost advantage; the product does not need it. |
| D10 | **IPOs are rules + LLM watchlist, never ML-scored.** | No price history exists for IPOs; honesty is a feature. |

---

## Phase 0 — Foundations (blockers; start immediately)

### 0.1 Market data — interim stance and migration plan

**Interim stance (now, while licensing is in progress):**
- **US**: continue on **yfinance + Finnhub** for development and early operation. Acknowledged as an interim, time-boxed arrangement — both must be replaced by licensed feeds before full commercial launch (yfinance/free-Finnhub terms do not cover commercial use).
- **EGX**: **scaffold only — no data implementation yet.** Build the full market structure (config, calendar, universe rules, model directories per 1.5) but leave the EGX data adapter unimplemented with an explicit marker in the code:
  ```python
  # markets/egx/data.py
  # TODO(EGX-DATA): EGX data adapter intentionally not implemented.
  # Awaiting licensed EOD feed (OHLCV + corporate actions + trading calendar).
  # Implement against the MarketDataProvider interface below when licensing lands.
  # See IMPLEMENTATION_PLAN.md § 0.1 for the migration checklist.
  ```
  The EGX market stays disabled behind config (`markets/egx/config.yaml: enabled: false`) so the pipeline, backend `market` tagging, and strategy-engine market split are all built and tested US-first, and EGX activates by flipping config + dropping in the adapter.

**Build now regardless (so the swap is trivial later):**
- `MarketDataProvider` abstraction in the pipeline — one interface (`get_ohlcv`, `get_calendar`, `get_corporate_actions`, `get_news`, `get_analyst`), per-vendor adapters behind it. yfinance/Finnhub become just the first two adapters; **no vendor names hard-coded anywhere else** in the pipeline.
- **Macro series** (freely available now): EGP/USD, CBE policy rate, inflation prints, T-bill yields (CBE publishes these) — needed for 1.3 features regardless of vendor.

**Migration plan — when licensed feeds arrive (per market):**
1. **Implement the adapter** against `MarketDataProvider` (vendor SDK/API → canonical OHLCV/calendar/actions schema). Nothing outside the adapter changes.
2. **Parallel-run validation (2–4 weeks)**: run old and new adapters side by side; nightly diff report on OHLCV values, missing days, split/dividend adjustments. Acceptance: <0.1% value discrepancies, all corporate actions reconciled.
3. **Re-fit data-dependent artifacts**: retrain scalers + model on the licensed history (vendor adjustment methodology may differ from Yahoo's), run the 1.8 backtest to confirm metrics are consistent.
4. **Cutover**: flip the vendor in market config; keep the old adapter dormant for one release as rollback.
5. **EGX activation specifically**: implement adapter → backfill history → run universe/liquidity rules → train the EGX model instance (1.5) → enable ingest with `market=EGX` → unhide EGX in the strategy engine's market split. Each step is independently verifiable.
- **Funds & ETFs** (with EGX licensing): NAV/price series for EGX ETFs (EGX30 tracker, gold ETFs) and money-market/fixed-income funds used by the stability sleeve — until then the stability sleeve is US-instrument-only (US ETFs), which the templates already tolerate via registry rules.
- **News** (with licensing): Arabic + English financial news covering EGX names, feeding 1.6.

### 0.2 Regulatory groundwork (parallel, non-engineering)
- FRA robo-advisory licensing consult (framework exists since ~2022). The outcome shapes wording ("advice" vs "decision support"), required disclaimers, suitability record-keeping, and what can be auto-recommended vs displayed.
- Brokerage/custody path for US securities for Egyptian users (partnership question; affects Phase 4 scope).
- Engineering consequence either way: **store every questionnaire answer, derived profile, recommendation, and rationale immutably** (audit trail). Cheap to build now, painful to retrofit.

### 0.3 Realized-outcome tracking (close the loop — highest ROI single item)
The DB already stores every DailyRun and prediction. Add:
- Nightly job: for each prediction whose horizon has elapsed, fetch realized price, compute realized return + direction hit, store on the prediction row.
- Rolling metrics table: directional hit-rate, rank-correlation (IC), by market/sleeve/risk-grade, over 30/90/365-day windows.
- This is simultaneously: model monitoring, the future "track record" marketing page, and the data needed to validate every model change in this plan.

```
StockPredictionOutcome
  prediction_id FK · realized_at · realized_return · direction_hit bool
  rank_percentile_predicted · rank_percentile_realized
```

---

## Phase 1 — Model & pipeline rebuild

### 1.1 Retarget to relative ranking (D2)
- New label per (stock, date): `return_30d(stock) − median(return_30d(universe, same date))`, computed within one market only. Optionally also the binary "beat median" for a classifier head.
- Training becomes per-market pooled cross-sections with strict chronological splits (keep the existing no-lookahead discipline).
- Output at inference: **relative-strength score → within-market rank**. UI/LLM language shifts from "+7.66% expected" to "expected to outperform the EGX universe / market."
- Honest metric from day one: hit-rate vs the 50% by-construction base rate, plus information coefficient (Spearman rank corr of predicted vs realized).

### 1.2 LSTM verdict (D8)
- Experiment: XGBoost/LightGBM on indicators+new features vs the full hybrid, on the new ranking target, walk-forward.
- Decision rule: hybrid stays only if it improves IC/hit-rate by a pre-agreed margin (e.g. +1.5pp hit-rate or +0.02 IC) on both markets. Otherwise ship trees-only: cheaper serving, no PyTorch/MC-Dropout/mkldnn constraints, simpler retraining.
- MC-Dropout confidence is replaced by **calibration** (1.4) regardless.

### 1.3 Feature expansion
All features that currently only gate the output (risk rules) go **into training**:
- News sentiment score (per 1.6), news volume/recency.
- Analyst consensus + PT upside — **US market only** (sparse/absent on EGX; the feature pipeline must tolerate missing blocks per market).
- Macro/regime block: EGP/USD level & change, CBE rate, T-bill yield, index trailing return & realized vol, VIX (US side).
- Market-structure block (EGX): distance to daily price limit, liquidity (avg daily traded value), zero-volume-day count.
- Calendar/fundamental light: earnings-date proximity, sector one-hot, market-cap bucket, P/E if licensed.
- Diagnostic gate for everything: XGBoost gain-based feature importance reviewed per training run; dead features get culled.

### 1.4 Probability calibration
- Post-hoc isotonic (or conformal) calibration on walk-forward validation outputs so "confidence 0.7" empirically ≈ 70%.
- Conviction/risk-grade formulas in `risk_rules.py` consume calibrated probabilities.

### 1.5 Per-market pipeline instances (D4)
```
Pipeline/
  core/            # shared: features, training, ranking, calibration, risk rules
  markets/
    us/   config.yaml   # calendar, universe rules, vendors, model dir
    egx/  config.yaml
  models/us/…  models/egx/…   # artifacts + scalers per market
```
- Two schedules (EGX run after EGX close Sun–Thu; US run after US close Mon–Fri). Two ingest calls to the backend, tagged `market`.
- Universe rules: EGX = EGX100 ∩ liquidity filter (min avg daily value traded, min trading days) — **defined now in config, activates with EGX data (0.1)**; US = keep the current yfinance large-cap screen for now, upgrade to licensed point-in-time index constituents at data migration (kills survivorship bias).

### 1.6 Arabic sentiment via Gemini (replaces FinBERT for EGX)
- **Build and validate the extractor on US/English news now** (Finnhub news, current source) so the code path is proven; point it at Arabic EGX news when the licensed feed lands (0.1).
- Daily batch: Arabic/English news per ticker → Gemini with structured output schema:
  `{ticker, sentiment: −1..1, confidence, event_tags[], summary_one_line}`
- `event_tags` vocabulary (fixed enum): earnings, product_launch, govt_contract, capital_increase, mgmt_change, regulatory, macro. These feed the catalyst sleeve (3.4) — one extraction, two consumers.
- Keep FinBERT for US or migrate US to the same Gemini extractor for one code path (preferred; benchmark cost first).
- Guardrails: schema-validated, cached, batch-rate-limited; sentiment stored with source article IDs for audit.

### 1.7 Walk-forward retraining + drift (kills the static-model problem)
- Monthly scheduled retrain per market on expanding window; strict versioning (`model_registry` table: version, train window, metrics, artifact hash).
- **Champion/challenger promotion**: new model ships only if it beats the incumbent on the most recent out-of-sample year (IC + hit-rate + turnover check).
- Drift alarms from 0.3's rolling metrics: alert when rolling 90-day hit-rate drops below (baseline − threshold); alert on feature-distribution drift (PSI) per feature block.

### 1.8 Backtesting engine (internal core capability)
- Walk-forward simulator: strategy template + model version → daily portfolio, with **transaction costs, EGX price-limit fills, and cash drag** modeled.
- Outputs per template: equity curve, CAGR, Sharpe, max drawdown, vs benchmarks (EGX30 TR, S&P 500, and **the ~20% EGP deposit rate** — the honest Egyptian benchmark).
- Every model/template change requires a backtest report before promotion. Also the source of marketing claims (with FRA-compliant presentation).

---

## Phase 2 — Questionnaire & profile engine (D7)

### 2.1 New questionnaire (8–10 questions, one per screen, ~2 min)
| # | Question captures | Drives |
|---|-------------------|--------|
| 1 | **Goal** — "What is this money for?" (retirement / long-term wealth / medium-term goal / speculation & learning) | Template selection (primary axis) |
| 2 | Horizon in years | Capacity + template |
| 3 | Amount + monthly contribution (y/n, size) | Position sizing, fractional constraints |
| 4 | Emergency fund exists? (y/n) | **Capacity gate** |
| 5 | Income stability (stable / variable / none) | Capacity |
| 6 | % of total savings this represents | Capacity gate |
| 7 | Market-reaction scenario (keep existing question) | Tolerance |
| 8 | Investment experience | Speculative-sleeve gate |
| 9 | Engagement — "daily / monthly / set-and-forget" | Monitoring cadence + notification plan |
| 10 | EGP-devaluation concern / comfort with USD assets | EGX-vs-US split, gold/US-ETF weighting |

### 2.2 Scoring engine (server-side, versioned)
- `RiskCapacity = f(horizon, emergency fund, income, savings-share)` and `RiskTolerance = f(reaction, experience)`; **effective risk = min(capacity, tolerance)**.
- Speculative sleeve unlocked only when: experience ≥ threshold AND capacity ≥ threshold AND user opts in with explicit "money I can afford to lose" confirmation.
- Everything stored: raw answers, scoring version, derived profile, timestamp. Immutable (append-only; re-taking creates a new version). This is the FRA suitability record.
- Frontend becomes dumb: renders questions, posts answers. **Remove the client-side risk calculation.**

### 2.3 Multi-goal schema (build now, even if v1 UI is single-goal)
```
Goal            (id, user_id, type, horizon_years, created_at)
QuestionnaireResponse (id, goal_id, answers jsonb, scoring_version, submitted_at)
InvestorProfile (id, goal_id, capacity, tolerance, effective_risk,
                 engagement, usd_comfort, speculative_unlocked, version)
Portfolio       (id, goal_id, template_id, status, …)   -- portfolio hangs off Goal, not User
```

---

## Phase 3 — Strategy engine & portfolio construction

### 3.1 Instrument registry (D3 — where "context-awareness" lives)
```
Instrument
  id · symbol · market (EGX|US) · type (stock|etf|fund|mm_fund)
  asset_class (equity|gold|fixed_income|cash_like)
  currency · realized_vol_1y · avg_daily_value_traded · dividend_yield
  suitable_for[] (stability|core|tactical|speculative)
  metadata jsonb (expense ratio, index tracked, fund manager, …)
```
- Nightly job refreshes computed stats (vol, liquidity). Rules read from here; nothing is hard-coded; Gemini receives registry rows as grounding context.

### 3.2 Strategy templates
```
StrategyTemplate
  id · name · goal_types[] · risk_range · engagement_range
  buckets jsonb        -- e.g. [{sleeve:"stability", weight:0.5, rules:{asset_class:["gold","fixed_income","cash_like"]}}, …]
  rebalance_cadence · drawdown_alert_pct · market_split_rules (EGX/US by usd_comfort)
```
Initial template set (v1):
| Template | Buckets (illustrative) | ML use | Cadence |
|----------|------------------------|--------|---------|
| Retirement / set-and-forget | 40% EGX index ETF · 25% gold ETF · 20% MM/fixed-income funds · 15% US ETF (if usd_comfort) | none daily | Semi-annual rebalance; drawdown alert −15% |
| Balanced growth | 50% core ranked equities (EGX+US split) · 30% stability · 20% cash ladder | ranking model | Monthly review |
| Active growth | 50% core ranked · 30% tactical (dip sleeve) · 10% speculative watch · 10% cash | ranking + signals | Weekly signals |
| Speculative (gated) | 40% core · 30% tactical · 20% IPO/catalyst · 10% cash | all sleeves | Daily/weekly |

### 3.3 Allocation optimizer (deterministic, D6)
- Inputs: template buckets, per-market rankings, instrument registry, user amount/constraints.
- v1 algorithm (simple, explainable): within each sleeve — take top-N by rank passing risk-grade filter → weight ∝ rank score / realized vol (inverse-vol tilt) → apply caps (max per position, max per sector, min position size given amount, fractional/lot rules) → normalize to bucket weight.
- Fully unit-testable; same inputs ⇒ same portfolio. Stored with inputs hash for audit.

### 3.4 Sleeve engines
- **Stability**: pure registry rules (asset_class + vol + liquidity). No ML.
- **Core**: ranking model top-quartile ∩ risk-grade ≥ threshold.
- **Tactical dip-buyer**: oversold (RSI / distance from 50-DMA) **AND** quality (profitability/debt if fundamentals licensed; else liquidity+size proxy) **AND** sentiment-not-deteriorating (1.6). Surfaced as "opportunities," bounded weight.
- **IPO/catalyst (D10)**: IPO calendar (licensed) + `event_tags` stream from 1.6 → rules produce a **watchlist with rationale**, clearly labeled speculative, never auto-allocated above template cap, gated per 2.2.

### 3.5 Monitoring engine (triggers, not schedules)
Extends existing Notifications module + Quartz:
| Trigger | Condition | Audience |
|---------|-----------|----------|
| Drawdown | portfolio −X% from high-water mark (X per template) | all, tone per profile |
| Market crash | index −Y% in 5 days or vol spike | all; retirement users get "context + hold" guidance |
| Drift | allocation deviates > Z pp from target | rebalance suggestion |
| Conviction reversal | held position's signal flips + sentiment deteriorates | active profiles |
| Periodic digest | quarterly (set-and-forget) / monthly (balanced) | per engagement answer |
- Each trigger → domain event → notification + Gemini-written contextual message grounded in the trigger data.

### 3.6 Gemini layer hardening
- **Context pack** per request: goal, profile, template, chosen portfolio + registry metadata, trigger context (if monitoring-initiated), realized track record snippet. Gemini explains/educates; it never chooses instruments or weights (D6).
- **Eval harness** (CI): golden-set prompts → assert schema validity, no out-of-universe tickers, no invented numbers (all numerics must appear in context pack), allocation echoes match optimizer output, tone rules (AR/EN both). Runs on every prompt or model-version change.
- Arabic + English output support (user language preference).

---

## Phase 4 — Backend & product integration (.NET)

- **Portfolio module** absorbs: Goal/Questionnaire/Profile entities (2.3), StrategyTemplate + Instrument registry (read models), Portfolio positions with target vs actual weights.
- **Recommendations module** becomes **Advisory module**: ingests per-market runs (`market` tag on DailyRun), calls optimizer, persists PortfolioProposal (immutable, versioned), exposes track-record queries from 0.3.
- **Notifications module**: new consumers for monitoring triggers (3.5).
- **New scheduled jobs** (Quartz): outcome-scorer (0.3), registry stats refresh (3.1), drift/drawdown scans (3.5), per-market pipeline fetch (existing job, duplicated per calendar).
- **API surface** (new/changed): `POST /goals` + questionnaire endpoints, `GET /portfolio/proposal`, `POST /portfolio/accept`, `GET /track-record`, `GET /watchlist/speculative` (gated).
- Frontend: new onboarding flow (one question per screen), goal dashboard (per-goal portfolio, next review date, track record), speculative watchlist UI behind gate, Arabic i18n groundwork.

---

## Phase 5 — Trust & go-to-market engineering

- **Public track record page** fed by 0.3/1.8: realized performance of each template vs EGX30 / S&P 500 / EGP deposit rate. Auditable methodology page alongside (FRA-compliant wording).
- Transaction-cost-aware advice: min amounts, lot sizes, spread warnings for thin EGX names.
- Model/methodology changelog (public): version, date, what changed — trust through transparency.

---

## Phase 6 — Bonus additions (post-v1, in priority order)

Not launch blockers. Each is scoped so it can be picked up independently once its dependencies exist.

### 6.1 Shadow-mode track record (start the week templates exist — cheapest trust asset we can build)
**What:** every strategy template runs as a live paper portfolio daily, from before we have users, so the public track-record page launches with months of real history.
**Implementation:**
1. `ShadowPortfolio (id, template_id, market_mix, inception_date, cash_balance)` + `ShadowPosition (portfolio_id, instrument_id, qty, avg_cost)` + `ShadowSnapshot (portfolio_id, date, nav, daily_return)` — plain tables in the Advisory module.
2. Nightly Quartz job (after the pipeline run): for each template → call the same allocation optimizer (§ 3.3) with a fixed notional (e.g. 100k EGP) → diff against current shadow positions → apply "trades" at the day's close price **with the same transaction-cost model as the backtester (§ 1.8)** → write snapshot.
3. Rebalances follow each template's real cadence rules — shadow portfolios must experience exactly what a user would, including drawdown alerts firing into a log.
4. Track-record page reads `ShadowSnapshot` aggregates; each series labeled "model portfolio, costs simulated" (FRA-safe wording).
**Dependencies:** §§ 3.2–3.3 running. Effort: ~2–3 days once the optimizer exists.

### 6.2 Data-quality gates + kill switch (before any real user relies on a run)
**What:** the pipeline refuses to publish a bad run; a human can hold or roll back any run.
**Implementation:**
1. `quality_gates.py` in `Pipeline/core/`, executed between scoring and ingest POST. Checks (all thresholds in market config):
   - coverage: scored tickers ≥ X% of universe;
   - staleness: max price date == expected trading day for that market's calendar;
   - sanity: no |change_pct| > Y% without a matching corporate action; feature blocks non-null above thresholds; sentiment feed non-empty if enabled.
2. On failure: run is written to disk + `POST /api/internal/daily-results?status=quarantined` (new status column on DailyRun) + ops alert (existing Notifications infra). Quarantined runs are invisible to the optimizer and users.
3. Kill switch: `DailyRun.status ∈ {pending_review, published, quarantined, rolled_back}` + a one-click admin endpoint to flip status. Config flag `RequireManualApproval=true` for the early months makes `pending_review` the default landing state.
4. The optimizer and recommendation queries always read "latest **published** run" — one WHERE clause makes the whole system kill-switch-aware.
**Dependencies:** none beyond current pipeline. Effort: ~2–3 days. Do this one first.

### 6.3 Feature-vector snapshotting (audit + debuggability; nearly free now, impossible later)
**What:** every prediction stores the exact inputs that produced it.
**Implementation:**
1. In `_predict_one`, serialize the final feature vector (post-scaling) + model version + scaler hash → compact JSON.
2. Add `features jsonb` + `model_version` to the prediction record schema (pipeline output + `StockPrediction` table). ~2–4 KB × 100 tickers/day — storage is a non-issue; add a 24-month retention/archival job later.
3. Reproduce endpoint (internal): given `prediction_id`, reload artifact `model_version`, re-run inference on stored features, assert equality — this is the audit answer and the debugging tool in one.
**Dependencies:** none. Effort: ~1 day. Fold into Phase 1 work if convenient.

### 6.4 RAG education assistant (the one place RAG belongs)
**What:** an Arabic/English Q&A bot ("هل الأسهم حلال؟", "what is an ETF?") that answers **only from our vetted content** — articles, Shariah board rulings, FRA-approved disclosures — never from the LLM's general knowledge. Serves the education funnel, in-app help, and the WhatsApp support channel.
**Implementation:**
1. Knowledge base: markdown docs in a `kb/` repo folder — `frontmatter: {id, topic, language, approved_by, review_date}`. Content pipeline = git PR + review (the approval workflow *is* git).
2. Indexing job: chunk (~500 tokens, heading-aware) → embed (Gemini embedding API) → store in **pgvector on the existing Postgres** (no new infra; a `kb_chunks (id, doc_id, lang, embedding vector, text, metadata)` table).
3. Query path: embed question → top-k by cosine (filter by language) → Gemini with system prompt: *answer only from provided sources; cite doc ids; if sources insufficient, say so and offer human support handoff*. Reuse the § 3.6 eval-harness pattern: golden questions asserting no-hallucination, correct refusals, tone.
4. Compliance guardrail: an out-of-scope classifier step — personal-advice questions ("should I buy CIB?") get a scripted redirect to the app's advisory flow, never a generated opinion.
5. Surfaces: in-app chat first; WhatsApp Business API later (same backend).
**Dependencies:** content existing (starts with ~30 docs from the § 3.2-content-engine articles). Effort: ~1–2 weeks incl. eval set.

### 6.5 Recurring investing / DCA engine — "الجمعية الرقمية" (digital gam3eya)
**What:** monthly auto-invest plans; the habit product. Framed in marketing as the gam3eya every Egyptian already trusts — pay yourself monthly.
**Implementation:**
1. `ContributionPlan (goal_id, amount, cadence, next_run_date, status)` — created from the questionnaire's contribution answer (§ 2.1 Q3), editable per goal.
2. Monthly Quartz job: on `next_run_date` → generate a **top-up proposal**: allocation of `amount` across the goal's current template weights (buy-only rebalance — new cash goes preferentially to underweight sleeves, minimizing sell-side churn and taxes/costs).
3. Scenario B (advisory-only): the proposal is a notification + one-screen "here's this month's plan — execute at your broker" checklist. Scenario A (integrated): one-tap execute, or fully automatic with standing consent (FRA wording matters here).
4. Missed-month handling: proposals accumulate; the next proposal absorbs unexecuted cash. Streak/consistency UI ("6 months of الجمعية") — habit reinforcement, shareable.
5. Marketing hook ships with it: gam3eya-framed content series + projected-value calculator ("500 EGP/month for 10 years = …", reusing the inflation-calculator component).
**Dependencies:** §§ 2–3 live. Effort: ~1 week backend + UI.

### 6.6 Zakat calculator (halal-mode companion; seasonal viral asset)
**What:** computes zakat due on portfolio holdings; standalone shareable tool + in-app feature. Spikes every Ramadan.
**Implementation:**
1. Rules engine (small, pure functions, Shariah-board-reviewed like § 2.1-halal screening): nisab threshold from live gold price (registry already tracks gold instruments); 2.5% on zakatable assets; per-madhhab toggle for the stock-zakat method (market-value vs zakatable-assets-ratio approach) — default to the board's chosen method, show the choice.
2. In-app: reads current positions + cash per goal → zakat due, with per-holding breakdown and an "export for your records" PDF.
3. Standalone page (like the inflation calculator, Vercel free tier): manual entry of holdings/gold/cash → same engine (shared TS package) → shareable result card for WhatsApp. Waitlist/download CTA.
4. Board sign-off on methodology text is the only external dependency — batch it with the halal-mode certification engagement.
**Dependencies:** none for the standalone; registry + positions for in-app. Effort: ~3–4 days + board review.

### Suggested pickup order
`6.2 quality gates → 6.3 snapshotting → 6.1 shadow mode → 6.5 DCA → 6.4 RAG assistant → 6.6 zakat` — gates and snapshotting protect correctness from day one; shadow mode starts the trust clock; DCA drives retention at launch; the assistant and zakat ride the content engine.

---

## Sequencing & dependencies

```
Phase 0:  0.1 data ──────────────┐        0.2 regulatory (parallel, external)
          0.3 outcome tracking ──┤  (0.3 needs nothing; do it FIRST)
                                 ▼
Phase 1:  1.1 ranking target → 1.2 LSTM verdict → 1.4 calibration
          1.5 per-market split ← 0.1        1.6 Gemini sentiment ← 0.1(news)
          1.7 retraining ← 1.1,0.3          1.8 backtester ← 1.1,0.1
                                 ▼
Phase 2:  questionnaire/profile engine (independent of Phase 1 — can run in parallel)
                                 ▼
Phase 3:  3.1 registry → 3.2 templates → 3.3 optimizer → 3.4 sleeves ← Phase 1 outputs
          3.5 monitoring ← 3.2      3.6 Gemini hardening ← 3.3
                                 ▼
Phase 4:  backend/product integration ← Phases 2+3
Phase 5:  trust layer ← 0.3, 1.8, live history accumulating
```

**Fastest meaningful milestone** (recommended v1): Phase 0.3 + Phase 2 + stability/core sleeves of Phase 3 on the **US market (yfinance + Finnhub interim data)** = a working goal-based product (retirement + balanced templates) with honest tracking — before the tactical/speculative sleeves. **EGX ships as scaffold only** (config, calendar, universe rules, disabled adapter per 0.1) and activates when licensed data lands — a data drop-in, not a rebuild. Everything after that is additive.

## Success metrics (define before building, measure from 0.3/1.8)

- Model: IC > 0.03 sustained; hit-rate vs median > 53% rolling 90d; calibration error < 5pp.
- Product: template backtests beat their honest benchmark (incl. deposit-rate comparison disclosed); drawdown alerts fire correctly in replay of 2020/2022 crashes.
- Ops: pipeline run success ≥ 99%; missed-run backfill < 24h; every recommendation reproducible from stored inputs.

## Explicit non-goals (agreed)

- No intraday/real-time anything (D9). No crypto/forex. No exotic deep models (TFT/Prophet/transformers) — benchmark-only if ever. No LLM-chosen allocations (D6). No ML-scored IPOs (D10). No un-gated speculative access.
