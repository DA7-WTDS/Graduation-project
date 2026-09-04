# QuantWise — Investor MVP Plan

**Goal:** a demo-able, diligence-ready product for investors within ~4 weeks.
**Positioning:** goal-based decision-support for Egyptian retail investors (advisory-only, Scenario B) — deterministic strategy engine + measured AI signal + LLM narration. Not a brokerage; no custody; FRA wording pending consult.

This document supersedes the graduation framing. It composes three workstreams into one schedule:

1. **Model truth** — serve the validated ranking champion, not the legacy hybrid (§A)
2. **Point-in-time replay** — manufacture an ~18-month out-of-sample track record without waiting in real time (§C)
3. **Sentiment-into-training A/B** — settle whether news/analyst features earn a place in the model, with a pre-registered gate (§D)

---

## 1. What already exists (build on, don't rebuild)

| Asset | Location | State |
|---|---|---|
| Ranking champion (trees-only, base-14, calibrated) | `Pipeline/models/ranking_v1/` | Validated: IC 0.077 (t≈9.2), hit 51.5%, ECE 3.8pp; **serving by default** since §A (`SERVING_MODEL=trees`) |
| Legacy hybrid LSTM+XGB serving path | `Pipeline/main.py` | Rollback only (`SERVING_MODEL=hybrid`); loads at startup so any snapshot replays |
| Goal questionnaire → profile engine | Portfolio module (`RiskScoring` v1, capacity/tolerance, append-only `InvestorProfile`) | Done |
| Strategy templates + deterministic optimizer | `StrategyTemplate`, `AllocationOptimizer` | Done |
| Proposal flow | `CreatePortfolioProposal` / `AcceptPortfolioProposal` / `GetPortfolioProposals` | Done |
| **Shadow paper portfolios (§ 6.1)** | `ShadowPortfolio/Position/Snapshot`, `ShadowPortfolioJob`, `/track-record` endpoint (with disclaimer) | Done — runs nightly on live runs |
| Outcome tracking + drift alarm | `ScoreOutcomesJob`, `PredictionOutcome`, rolling hit-rate alarm | Done |
| Monitoring triggers | MarketCrash / Drawdown / Drift / ConvictionReversal / Digest events → notifications | Done |
| Quality gates + kill switch | `core/quality_gates.py`, `DailyRun.Status` machine | Done |
| Audit trail | Feature snapshots, content-hashed `model_version`/`scaler_hash`, `/api/reproduce` | Done |

**Net:** the product loop exists end-to-end. The MVP gap is packaging + two surgical fixes + manufactured history.

## 2. Explicitly OUT of MVP (roadmap slides only)

EGX activation · speculative sleeve (stays gated-off) · DCA engine · zakat calculator · RAG assistant · multi-market allocation UI · intraday anything · public launch on yfinance/Finnhub (demo/private-pilot only until licensed feeds land per IMPLEMENTATION_PLAN § 0.1).

---

## 3. Week-by-week execution plan

### Week 1 — Model truth + clocks started

- [x] **Wire `ranking_v1` into serving behind config flag** (`SERVING_MODEL=trees|hybrid`, default trees) — ✅ done, see §A implementation record:
      - Trees-only hot path: raw base-14 → XGBoost rank score → isotonic ⇒ conviction = P(beat median). No torch in the request path.
      - Hybrid path intact behind the flag; both stacks load so any snapshot replays.
      - `change_pct` semantics now relative-to-median in trees mode — pipeline DTOs, the Gemini prompt and the frontend all updated (follow-up 2 in §A, resolved 2026-09-04).
- [x] **Retire MC-dropout pseudo-confidence from user-facing output** (kept internally for the hybrid lane only).
- [x] **Full-universe sentiment gathering live** — ✅ done 2026-09-04. The moat clock is
      started; it only accrues once the stack is actually deployed (next item).
      - The top-35 shortlist is gone: `/api/score` now gathers sentiment for every scored
        name. A panel built from the shortlist would have been conditioned on the model's
        own output — only names it already liked — so a feature learned from it would be
        measuring the selection as much as the sentiment, and § D could not be answered
        with it. `SENTIMENT_TOP_N` (default 0 = all) keeps a cap as a rate-limit escape
        hatch; a capped run logs a warning saying the panel is biased that day.
      - `core/sentiment_panel.py` appends one row per (ticker, date) as hive-partitioned
        Parquet under `SENTIMENT_PANEL_DIR` (`training/data/sentiment_us/date=YYYY-MM-DD/`).
        One file per day, never rewritten — appending into a single `history.parquet` as
        originally sketched means rewriting the whole file every run, making each run a
        chance to corrupt the entire history. Re-running a day replaces only that day's
        partition, so the job stays idempotent under retry.
      - Rows store the RAW components (consensus / actions / price_target / news) plus the
        composite. The composite's weights are a serving decision that will change; a
        stored composite alone could never be re-derived under different weights.
      - Written BEFORE risk rules and gates: what the vendors said today stays true and
        worth keeping even when the run is later quarantined for a price-data problem.
        Best-effort — a panel write can never cost us the run.
      - Persistence: `docker-compose` mounts a `sentiment_panel` volume. Inside the
        container filesystem the history would reset on every restart while still looking
        like the clock was running. `/health` now reports `sentiment_panel` (days, first,
        last) so a stalled clock is visible where uptime checks already look.
      - **Caught in passing:** this change would have broken the nightly run. Sentiment
        time is vendor-throttle-bound, and the full universe costs 100 × 3 Finnhub calls
        × 1.05s = **315s of pure sleep**, before FinBERT, the batch download, or any
        inference — already past the backend's 300s `/api/score` timeout. Raised to 1800s;
        nothing waits on it (nightly, `[DisallowConcurrentExecution]`), whereas a short
        timeout costs the whole day's run and its panel row.
      - **New:** `Pipeline/test_sentiment_panel.py` (9 tests) covering partition-per-day,
        retry idempotency, raw-component retention, null components, schema stability and
        the cold-start read. Pipeline suites now 47 green (9 + 12 + 10 + 16).
- [ ] **Nightly reliability**: deploy stack to VPS via existing docker-compose; ops alert (email)
      on quarantined runs, failed jobs, drift alarm (<45% 90-day hit-rate).
      - ✅ **Ops alerting done 2026-09-04.** Every operational alert now goes through one
        `IOpsAlert` path that writes an in-app notification for each Admin **and emails**
        them. The in-app bell alone is a pull channel: it only works if someone happens to
        open the app, which is exactly not the case on the night a run is quarantined.
        Delivery is best-effort per channel and per recipient — a dead SMTP host cannot stop
        the notification being written, and one bad address cannot stop the other admins
        being told. With no Admin users at all the full alert text is logged at warning,
        since that is the case most likely to vanish silently.
      - ✅ **Drift alarm now actually alarms.** `ScoreOutcomesJob.CheckDriftAsync` previously
        wrote a `LogWarning` and published nothing, so the § 1.7 alarm reached a log file
        nobody was tailing. It now raises `ModelDriftDetectedIntegrationEvent` — and does so
        **on the crossing night only**, via `MonitorRules.DriftCrossed`, matching the crash
        rule. A level test would have emailed every night for as long as the model stayed
        bad, and an alert that arrives nightly is one nobody reads. "Yesterday's view" is
        reconstructed from `PredictionOutcome.ScoredAt` rather than stored, so the check
        holds no state that could drift out of sync with the outcomes table.
      - 6 new `MonitorRulesTests` cover crossing, persistence, recovery-then-relapse, the
        exactly-on-floor boundary, and the first night the window becomes measurable while
        already bad (which must fire). Backend suites 188 green.
      - **Remaining: the deploy itself.** `docker compose up -d --build` on an always-on
        host — that is what starts both moat clocks, and it needs your VPS.
- [x] Run the **Finnhub depth probe** (§C step 0) — results in §C.0; news ≈12-month horizon + ~250-item cap, yfinance actions to 2012, consensus ~4 buckets.

### Week 2 — Manufactured history + trust surface

- [x] **Replay corpus builder** — ✅ done 2026-09-04, `replay/build_corpus.py`.
      Written and validated live; the full 100-ticker fetch still has to be run (hours of
      throttled calls) — it is resumable, so it can go overnight in stages.
      - **Adaptive pagination is not optional, and now proven.** A live AAPL run over 60 days
        returned **2,711 headlines from 31 calls**; a single naive call returns ~244 (the cap).
        Without cap-splitting the corpus would silently have been missing ~89% of its news,
        with no error anywhere. Slices halve and recurse whenever a response comes back at
        the cap, and stop subdividing at one day.
      - Analyst actions come from yfinance (Finnhub's endpoint is premium): verified 972 rows
        for AAPL back to 2012-09, covering the entire replay window.
      - **Price targets are not collected at all**, per § C.2 rule 3 — vendors expose only the
        current target, so using it at a past date leaks. Recorded in the manifest as a
        deliberate omission so nobody later "fixes" it.
      - Resumable: each ticker's shard is written as it completes and skipped on re-run
        unless `--refresh`. A manifest records per-ticker counts, call counts, shard hashes,
        and the **measured** date news actually starts — a claim the methodology page needs
        to be able to state precisely rather than as "about twelve months".

      **Measured constraint this settles:** the replay window is 2024-12-31 — today (612
      days), but Finnhub retains ~12 months of news, so **~40% of replayed dates have no news
      component at all**. § C.1 anticipated this; it is now quantified. Those dates score
      through a composite renormalized over the surviving components, which is exactly why
      the composite had to become shared code (below) rather than a replay-only copy.

      - **Foundation, done first:** `core/sentiment_scoring.py` now holds the weights,
        thresholds and the composite itself, used by BOTH `main._score_gathered` and the
        replay scorer. § C.2 rule 3 requires "identical windows and weights"; two copies that
        agree today are not that. Verified behaviour-identical against the pre-extraction
        output, and `test_sentiment_scoring.py` pins it with an independent reimplementation
        of the original arithmetic so a later "simplification" cannot quietly change what a
        score means.
      - **New:** `test_replay_corpus.py` (9 tests, no network) — cap-splitting recovers the
        full set, slices tile the window with no gaps, dedup across overlaps, a transient
        call failure does not lose the rest, and recursion terminates on a capped single day.
        Pipeline suites now 65 green.

      Note: the committed `depth_probe.json` reads as all-zeros for news and actions, which
      is correct and not a bug — its "recent" window (2024-11 — 2025-01) is now ~21 months old,
      past the retention horizon, and its action probe measures the premium Finnhub endpoint
      rather than the yfinance substitute § C.0 actually recommends. Live re-checks confirm
      both sources work.
- [ ] **Fast-lane replay engine** (`Pipeline/replay/`): point-in-time features → batch champion scoring over OOS window → `ScoreRecord`-shaped Parquet per date (price+news+actions+consensus; PT excluded).
- [ ] **Fidelity lane**: `market=us_sim` ingest (separate key), `DailyRun.Status = Simulated` (never servable), date-parameterized `ShadowPortfolioJob` consuming sim runs with historical fills via `/api/closes`; instant outcome marking (horizons elapsed); notifications gated off for `us_sim`.
- [x] **Track-record UI page** — ✅ done 2026-09-04. Public route `/track-record`,
      anonymous like the endpoints behind it (aggregates only, no user or position data).
      - Two tabs, because there are genuinely two track records and they answer different
        questions: **model portfolios** (what a portfolio following each strategy did, costs
        simulated) and **prediction accuracy** (how often the daily signal was directionally
        right). `/api/shadow-track-record` existed since § 6.1 but nothing rendered it.
      - The backend's disclaimer is printed verbatim — it is FRA-safe wording, not copy for
        the frontend to improve on. The accuracy tab states the 50%-by-construction base rate
        next to the hit rate, so 51.5% cannot read as a 51.5% win rate.
      - NAV curves are inline SVG (no chart dependency), scaled to each series' own min/max
        rather than to zero — these move a few percent and a zero axis flattens them all into
        the same line. Rebalance days are marked: that is when costs were charged, and the
        reader should see what they paid for. Losing series stroke red and stay on the page.
      - Not in the authed nav: the page is public and chrome-less, so a nav entry would have
        dropped a signed-in user out of the app shell mid-session. Reached instead from the
        Plan page's existing track-record card, which is where the § 5 demo already goes.
      - **SIM/LIVE provenance is deliberately absent** — there are no simulated segments until
        the § C replay lane exists. Adding a provenance flag now would mean shipping a legend
        for a distinction the data cannot yet make.
- [x] Methodology page — ✅ done 2026-09-04, at `/methodology` and linked from the track
      record. Metrics, definitions and the limitations list, every figure traceable to a
      checked-in artifact file named inline (`metrics.json`, `backtest.json`,
      `lstm_experiment.json`, `registry.json`). Explains the relative-ranking target and why
      the absolute-return one was abandoned; lists what runs nightly (gates, outcome scoring,
      drift alarm, audit snapshots, champion/challenger). The six known limitations from § 6
      are stated in full, including the two that are unflattering: a naive momentum baseline
      beats the model on decile spread, and the backtest trails an equal-weight basket on
      Sharpe. Static in the bundle, since these are versioned facts that change with the
      artifacts, not live data.
      - **Verified in a browser**, not just built: both pages rendered against a stubbed API
        (the two endpoints are anonymous, so no auth was needed). All three NAV curves drew
        with the right point counts and rebalance markers, the losing portfolio stroked red,
        both tabs populated, and the responsive rules held. A JSX whitespace bug that ran
        "flagged" into a `<code>` element was caught and fixed this way.

### Week 3 — Sentiment A/B + demo hardening

- [ ] **`build_sentiment_panel.py`**: as-of aggregation of corpus ledgers into a (ticker, date) feature panel (§D).
- [ ] **`build_dataset.py --with-sentiment`** + third variant in `train_ranking.py` (identical-row-subset A/B, walk-forward folds); pre-registered keep rule; registry entry either way.
- [ ] Staging hardening: seeded demo account(s), stable data, k6 smoke, Arabic/EN toggle QA.
- [ ] Demo script written + rehearsed against staging (see §5 narrative order).

### Week 4 — Pitch kit + consistency pass

- [ ] Metrics/deck slides generated from real artifacts (`metrics.json`, `backtest.json`, `registry.json`) — no hand-typed numbers.
- [ ] Data room: SYSTEM_ARCHITECTURE, DATABASE_ERD, IMPLEMENTATION_PLAN, this doc, methodology page export.
- [x] **Docs consistency fix** — ✅ done 2026-09-04. README's AI section rewritten around
      ranking: the "30-day RMSE 0.0949" headline is replaced by a metrics table sourced
      from `ranking_v1/metrics.json` (IC 0.077 t≈9.2 · hit 51.5% vs 49.7% base rate · ECE
      3.8pp), each against a naive momentum baseline. Adds why the absolute-return target
      was abandoned and why the LSTM was dropped (−0.025 IC). The backtest is cited *with*
      its caveats — survivorship bias, and trailing equal-weight on Sharpe — per § 5's "lead
      with process, never headline the +92%". Intro, tier table, flow diagram, phase list,
      endpoint list, testing bullet and repo tree all follow. The IEEE paper stays listed,
      relabelled as documenting the earlier hybrid with a pointer to the change rationale.
- [ ] Dry-run investor session; freeze demo environment.

---

## A. Surgical fix details — serving the champion ✅ IMPLEMENTED 2026-08-24

**Status:** shipped behind `SERVING_MODEL` env (`trees` default | `hybrid` rollback). All 26 pipeline tests pass (incl. 5 new trees-mode tests); live-artifact smoke verified a snapshot→reproduce round-trip (`matches=true`) through both stacks.

Implementation record & decisions:

- **Both stacks always load at startup**, whichever flag is active — so `/api/reproduce` can replay *any* historical snapshot (trees or hybrid) under either flag value. Torch stays out of the hot path regardless (mode picks the infer function).
- **Snapshot schema extended**: `"mode": "trees" | "hybrid"`; absent mode ⇒ legacy hybrid row (backward compatible — old stored snapshots keep replaying).
- **Feature order discipline**: trees path feeds indicators in `ranking_v1/features.json` order (`RANKING_COLS` — same set as `TECH_COLS`, *different* ordering than `universal_config.json`; training order wins). No scalers involved: champion consumes raw indicator values.
- **Confidence semantics replaced**: MC-dropout pseudo-confidence retired from user output; `confidence` = isotonic-calibrated P(beat median). Observed live range ≈ [0.458, 0.596].
- **`scaler_hash` is null in trees mode** (nothing scaled); response models made nullable. Content-hashed `MODEL_VERSION` now derives from `xgb_ranking.json + calibrator.pkl + features.json`.
- **Semantics shift (§ 1.1)**: `change_pct` = expected return relative to universe median; direction = vs-median out/underperform. DTO docstrings updated.
- Rollback = set `SERVING_MODEL=hybrid` (compose passes `Pipeline/.env`; added to `.env.example`).

Follow-ups surfaced by the swap:

1. ✅ **RESOLVED 2026-09-04 — risk-rule thresholds retuned (rank-based).** The two
   absolute cutoffs both broke under calibrated relative scores. Measured over the
   champion's own 35,515-row OOS slice: `|change_pct| < 1.5` fired on **95.9%** of
   records and `confidence < 0.30` on **0.0%** — one leg flagged nearly everything, the
   other nobody, so `low_conviction` carried no information in either direction.

   **Implementation:** `low_conviction` is now decided by RANK within the run rather
   than by a fixed cutoff (`LOW_CONVICTION_QUANTILE = 0.10` per leg, union of the
   confidence and |score| legs). Re-fitting constants would only have bought time:
   `retrain.py` promotes monthly on IC, which is scale-free, so nothing holds the
   score's SCALE stable across champions and any constant rots at the next promotion.
   Ranking is immune to that and is the right question for a ranking model anyway —
   *is this name distinguishable from the rest of today's cross-section?* One code
   path now serves both stacks.

   Records TIED with the cutoff are excluded rather than truncated to a fixed count.
   Calibrated confidence is heavily tied (isotonic emits steps; a quarter of a run can
   share one probability) and a fixed count would have flagged some tied names and not
   others on alphabetical order alone — indefensible in an audit. A test caught exactly
   that defect in the first draft.

   **Calibrated against evidence, not taste:** 0.10 per leg reproduces the ~10% firing
   rate the original thresholds were built around (measured from the last hybrid live
   run, where the flag fired on 10/100). Replaying 360 daily cross-sections of the OOS
   slice gives **9.5% mean (7.1—14.1%)**, versus 95.9% before. The same code on the
   hybrid run gives 18% rather than 10%; accepted deliberately — the tuning target is
   the stack that actually serves, and a rollback erring toward MORE caution is the
   safe direction.

   **New:** `Pipeline/test_risk_rules.py` — 12 tests, the module's first. Covers both
   original scale bugs as regressions, magnitude-not-sign ranking, scale invariance
   under a 100× score change, tie handling, and the small-run fallback to the absolute
   cutoffs (kept for single-record callers). Pipeline suites now 38 green (12 + 10 + 16).
2. ✅ **RESOLVED 2026-09-04 — relative-return semantics reached the LLM and the UI.**
   Worse than stale copy: `frontend/src/pages/Simulator/Simulator.jsx` was computing
   `invested * (1 + changePct / 100)` and printing the result as a projected dollar
   value, turning a vs-median ranking score into a cash forecast. The Gemini prompt fed
   the same field as `change={ChangePct}%` under a *30-day horizon* header.

   **Implementation:** a new `PredictionScale` helper reads the serving mode from the
   § 6.3 feature snapshot already stamped on every prediction (`"mode": "trees" |
   "hybrid"`) — no schema change, no new config, and a rollback re-labels the prompt
   and the UI by itself with nothing to remember. Absent snapshot ⇒ absolute, matching
   the pipeline's own convention for legacy rows.

   - **Gemini prompt**: candidates now render as `rel=+0.42pp` under a header stating the
     score is expected return versus the median stock and *NOT a forecast of the share
     price*; hybrid keeps `chg=X%`. New System rule 10 forbids target prices, monetary
     gains, guaranteed returns, and converting the score into an amount of money.
   - **API**: `GET /api/predictions` gained `scoreScale: "relative" | "absolute"`.
   - **Simulator**: under a relative scale it shows weighted expected out/under-performance
     in pp and drops the projected-value and P/L tiles entirely; per-row it shows capital
     allocated, not an invented destination value. Absolute mode is untouched. The flag
     defaults to relative when `scoreScale` is missing, so the fail-safe direction is the
     one that shows no dollar figure.

   Backend builds clean; `vite build` passes. Not yet verified in a browser — the page
   needs a live run behind auth, and no stack is deployed yet.
3. `/health` now reports `serving_model`, `model_version`, ranking stack status.

4. ✅ **RESOLVED 2026-09-04 — pipeline image could not boot.** The swap made
   `models/ranking_v1/` a hard import-time requirement (`main.py` raises
   `FileNotFoundError` without `features.json`), but `Pipeline/Dockerfile` still
   copied only the six legacy hybrid artifacts — so `docker compose build pipeline`
   produced an image that died on startup, blocking the VPS deploy and both week-1
   moat clocks with it.

   **Implementation:** added a `COPY models/ranking_v1/{xgb_ranking,calibrator,features}`
   instruction ahead of the hybrid block, with a comment recording *why* both stacks
   ship (either flag value must be able to replay either snapshot mode). Only the three
   runtime files are copied — `metrics.json`, `backtest*`, `feature_importance.json` and
   `lstm_experiment.json` stay out of the image, consistent with the existing policy.

   **Verified** without a Docker daemon by reconstructing the container's `/app` layout
   from the `COPY` instructions alone and importing `main` against it: both
   `_load_ranking()` and `_load_hybrid()` succeed, `SERVING_MODEL=trees`,
   `MODEL_VERSION=c999fa79ac2f342d`, `scaler_hash=None`, 14 ranking columns, trees
   inference returns, and a snapshot → `/api/reproduce` round-trip reports
   `matches=true, model_version_matches=true`. Repo test suites still green: 26/26
   (16 reproduce + 10 quality-gate). A full `docker build` remains unrun — daemon was
   down — so the first VPS build is still the real confirmation.

## B. Data moat clocks (start in week 1, compounding always)

1. **Full-universe daily sentiment panel** (Parquet, append-only, partitioned by date).
2. **Shadow track-record age** on production infra — gaps look worse than shortness.
Both are investor-facing assets precisely because they cannot be bought retroactively.

---

## C. Point-in-time replay system

Principle: every stage of the daily run must be a function of data available at replay date *t* — identical code paths to live, differing only in data sourcing.

### C.0 Probe first — RESULTS (measured 2026-08-24, free-tier key)

`training/probe_finnhub.py` → `training/data/replay_corpus/depth_probe.json`

| Endpoint | Measured reality |
|---|---|
| `/company-news` | **~12-month rolling retention**; windows fully older → empty. **~250-item cap per call** returning only the newest tail ≤ `to` ⇒ corpus builder must paginate adaptively (narrow slices while count hits cap). Rich coverage within horizon (~240+/mo for AAPL). |
| Analyst actions | Finnhub `/stock/upgrade-downgrade` = premium (HTTP 403). **yfinance `get_upgrades_downgrades` substitutes: full ledger back to 2012** (AAPL: 970 rows) — entire OOS window covered, free. |
| Consensus | `/stock/recommendation`: only ~4 monthly buckets (2026-05→2026-08) on this key. Near-zero history. |
| Price targets | Premium/current-only → excluded regardless (leakage rule). |

Rate limit confirmed: 60/min (`X-Ratelimit-*` headers), throttle holds.
Side finding for the live path: `_finnhub_raw_headlines`'s 14-day ask silently truncates to the newest 150 items (cap) — acceptable live, now documented.

### C.1 Fidelity map (post-probe)

| Stage | Historical source | Fidelity |
|---|---|---|
| Universe | Today's screener | Degraded — survivorship, disclosed |
| OHLCV → features | yfinance full history | **Exact** (all features backward-looking) |
| Model prediction | static artifacts | **Exact** |
| News headlines | `/company-news`, trailing ~12 months only | High inside horizon; **absent before it** (component drops out via weight renormalization) |
| Analyst actions | yfinance ledger (2012+) | **High — full OOS window** |
| Consensus | monthly buckets ≤ t (~4 months deep) | Negligible — effectively absent in replay |
| Price targets | current-only / premium | **Excluded — leakage guard** |

Consequence: replayed sentiment is **actions-everywhere + news-trailing-12-months**, not uniform. The sentiment-into-training A/B overlap window (§D) is therefore ~12 months of news-bearing rows; walk-forward mandatory.

### C.2 Non-negotiable rules

1. **OOS-only replay**: replay window starts at the chrono-split test boundary (`registry.json.test_slice_from` = 2024-12-31). Anything older is in-sample memorization.
2. **As-of cutoff convention** identical to live post-close run (~01:00 UTC next day) — never naive midnight.
3. **PT component excluded** from replayed composites; weights renormalized over present components (merge logic already supports this).
4. **No strategy-layer reimplementation in Python for the product number** — the fidelity lane goes through the real .NET optimizer/shadow jobs. Fast lane (pure Python) is for research iteration only, clearly labeled.

### C.3 Architecture

```
corpus builder (one-time)          fast lane (research)            fidelity lane (product)
/company-news     ─┐                                        ┌─> POST /api/internal/daily-results
/upgrade-downgrade ├> replay_corpus/*.parquet ─> PIT scorer ─    market=us_sim, key=sim-key
/recommendation   ─┘        + OHLCV cache       (shared w/ live ─> Status=Simulated
                                            semantics)          ─> ShadowPortfolioJob(date-param)
                                                                ─> ScoreOutcomesJob (instant)
                                                                ─> ShadowSnapshot (provenance=SIM)
```

- PIT scorer = refactored `_score_gathered`: pure function over ledger slices `≤ t_close`, identical windows (14d news / 30d actions) and weights.
- FinBERT batched over globally-deduped unique headlines (overnight CPU OK).
- Track-record page renders one continuous series: `[SIM segment][LIVE shadow segment]`, provenance-flagged.

### C.4 Instrumented honesty

Run live AND same-day replay going forward; publish the measured live-vs-replay divergence after ~60 days on the methodology page. Weakness converted into a quantified caveat.

---

## D. Sentiment-into-training A/B (pre-registered)

### D.1 Design

- Panel: whole-universe (corpus is unbiased across tickers — top-35 bias dies here), as-of merged, strict cutoffs, PT excluded, headlines deduped per window.
- **Identical row subsets** for all variants (availability itself must never become a feature/confound) — same discipline as `experiment_lstm.py`.
- Variants: `base_14` (control) vs `base_14 + sentiment_block`, identical splits/hyperparams/purge; walk-forward folds (rolling origin) because the overlap window is short.
- **Keep rule (pre-registered before first run):** keep iff ΔIC ≥ +0.005 AND Δhit ≥ 0 across folds. Otherwise cull and record the negative result.

### D.2 Feature block

`sentiment_composite` (renormalized, no PT) · `|composite|` · sentiment momentum (Δ vs 5d mean) · `log_news_count` · `has_news` · recency-weighted `action_score` · `days_since_latest_action` · `consensus_score` (bucket staleness documented) · continuous component-disagreement.

FinBERT locally (matches live US path; zero cost). Gemini extractor stays EGX/Arabic-future.

### D.3 Expectations & fallbacks

Literature prior: +0.005–0.01 IC at 21d horizon; may fail the gate. Even then: block remains required input for tactical dip-sleeve ("sentiment-not-deteriorating") and future catalyst watchlist; a clean negative registry entry is diligence credibility.

---

## 5. Investor narrative (demo order)

1. **Hook** — Egyptian retail has deposits, gold, and rumors; we give goals→portfolios with measured advice. Live: questionnaire → proposal → LLM-explained portfolio (AR/EN).
2. **Trust as product** — track-record page (realized + shadow, SIM/LIVE flagged) → methodology: IC, hit-rate vs 50% base rate, calibration error 3.8pp, cost-aware OOS backtest, quarantine kill-switch, reproduce-endpoint audit. **Lead with process, never headline the +92% survivorship-flattered backtest.**
3. **Moat** — proprietary accumulating panel (started week 1), immutable suitability audit trail (FRA-ready), provider abstraction making licensed-data cutover a config flip.
4. **Roadmap** — EGX on licensing (scaffold ready), sleeves, DCA/zakat/RAG as post-v1.
5. **Ask** — FRA consult, licensed feeds, content-engine CAC (education-first funnel per BUSINESS_PLAN).

## 6. Known risks / disclosed limitations (methodology page source)

Survivorship-biased universe (levels inflated equally; cross-sectional skill possibly more) · SIM segments exclude PT component, consensus monthly-stale · live-vs-replay divergence being measured · hit-rate 51.5% below 53% internal target · naive momentum beats model on decile spread (documented) · interim vendors not commercial-licensed · advisory-only until FRA clarity.

## 7. Definition of done

- [ ] Production serves calibrated ranking champion; rollback flag tested
- [ ] Nightly pipeline green ≥ 10 consecutive days on VPS; alerts fire on induced failure
- [ ] Track-record page shows SIM(≥15 months)+LIVE series with provenance + disclaimer
- [ ] Sentiment A/B executed; registry entry exists (positive or negative)
- [ ] Full-universe sentiment Parquet accruing daily since week 1
- [ ] Demo account walkthrough < 10 minutes, rehearsed, Arabic + English
- [ ] Deck numbers traceable to artifact files; README consistent with ranking framing
