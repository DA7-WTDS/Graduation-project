# QuantWise — Unified Pipeline Service
# FastAPI service: fetch tickers → prediction → FinBERT sentiment → risk rules.
# Exposes POST /api/score, called once per day by the .NET FetchDailyPipelineJob Quartz job.
#
# SERVING MODEL (MVP_PLAN § A): two inference stacks coexist, selected by
# SERVING_MODEL env ("trees" default | "hybrid"):
#   • trees  — models/ranking_v1 champion: XGBoost rank score over raw base-14
#     indicators, isotonic-calibrated conviction = P(beat universe median).
#     change_pct is RELATIVE to the per-date universe median. No torch in the
#     hot path.
#   • hybrid — legacy absolute-return LSTM→XGBoost head with MC-dropout
#     stability confidence. Kept behind the flag for rollback/comparison.
# Both artifact sets load at startup either way, so POST /api/reproduce can
# replay ANY historical snapshot (either mode) under today's process.
#
# IMPORTANT: torch.backends.mkldnn.enabled = False must stay.
# nn.LSTM's oneDNN kernel is non-deterministic off the main thread; disabling it
# forces the thread-safe native RNN path.

import hashlib
import json
import logging
import os
import pickle
import threading
import warnings
from concurrent.futures import ThreadPoolExecutor, as_completed
from contextlib import asynccontextmanager
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Literal

import numpy as np
import pandas as pd
import torch
import torch.nn as nn
import xgboost as xgb
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from core.data_provider import get_provider
from core.features import compute_features
from core.lstm import LSTMBackbone
from core.quality_gates import run_quality_gates
from core import sentiment_scoring
from core.sentiment_panel import append_daily, panel_summary
from core.news_store import append as store_news, normalize as normalize_news, summary as news_summary
from risk_rules import apply_risk_rules

warnings.filterwarnings("ignore")

torch.backends.mkldnn.enabled = False

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
log = logging.getLogger(__name__)


BASE_DIR   = Path(__file__).parent
MODEL_DIR  = BASE_DIR / "models"

CONFIG_PATH          = MODEL_DIR / "universal_config.json"
LSTM_PATH            = MODEL_DIR / "lstm_backbone.pth"
XGB_PATH             = MODEL_DIR / "xgb_head.json"
FEATURE_SCALER_PATH  = MODEL_DIR / "global_feature_scaler.pkl"
TECH_SCALER_PATH     = MODEL_DIR / "global_tech_scaler.pkl"
TARGET_STATS_PATH    = MODEL_DIR / "target_stats.json"

RANKING_DIR           = MODEL_DIR / "ranking_v1"
RANKING_XGB_PATH      = RANKING_DIR / "xgb_ranking.json"
RANKING_CALIBRATOR_PATH = RANKING_DIR / "calibrator.pkl"
RANKING_FEATURES_PATH = RANKING_DIR / "features.json"

with open(CONFIG_PATH) as f:
    CONFIG = json.load(f)

LOOK_BACK    = CONFIG["look_back"]       # 60
FEATURE_COLS = CONFIG["feature_cols"]    # 5 LSTM input features
TECH_COLS    = CONFIG["tech_cols"]       # 14 tech indicator features
LSTM_PARAMS  = CONFIG["lstm_params"]

# Serving-mode flag (MVP_PLAN § A). Default: the validated ranking champion.
SERVING_MODEL = os.getenv("SERVING_MODEL", "trees").strip().lower()
if SERVING_MODEL not in ("trees", "hybrid"):
    raise RuntimeError(f"SERVING_MODEL must be 'trees' or 'hybrid', got {SERVING_MODEL!r}")

if not RANKING_FEATURES_PATH.exists():
    raise FileNotFoundError(
        f"Ranking champion features missing: {RANKING_FEATURES_PATH}. "
        "models/ranking_v1/ is required for trees serving and snapshot replay.")
RANKING_COLS: list[str] = json.loads(RANKING_FEATURES_PATH.read_text(encoding="utf-8"))

# Minimum raw OHLCV rows needed before feature engineering:
#   60 (look_back) + 35 (indicator warmup: SMA_30 + momentum lags) + 10 buffer
MIN_RAW_ROWS = 120


MC_SAMPLES   = 30
MC_SEED      = 1234
Z_REF        = 1.0
MC_STD_REF   = 0.015

# Feature-vector snapshotting (§ 6.3). Model identity is content-addressed —
# hashed from the artifacts actually loaded — so it can never drift out of sync
# with a hand-maintained version string. Snapshots carry "mode" so /api/reproduce
# knows which stack to replay through ("hybrid" when absent = legacy rows).
# trees mode uses no scalers, so scaler_hash is null there; a scaler swap still
# invalidates stored hybrid vectors via its own hash.
SNAPSHOT_SCHEMA  = 1
SNAPSHOT_DECIMALS = 6  # features live in ~[-3,3] (scaled) or raw indicator range; 6dp reproduces to 4dp output


def _sha256_files(*paths: Path, length: int = 16) -> str:
    h = hashlib.sha256()
    for p in sorted(paths, key=lambda x: x.name):
        try:
            h.update(p.read_bytes())
        except OSError as e:
            log.warning(f"hash: could not read {p.name} — {e}")
            h.update(p.name.encode())
    return h.hexdigest()[:length]


if SERVING_MODEL == "trees":
    MODEL_VERSION = _sha256_files(RANKING_XGB_PATH, RANKING_CALIBRATOR_PATH, RANKING_FEATURES_PATH)
    SCALER_HASH: str | None = None   # no scaling in the trees path
else:
    MODEL_VERSION = _sha256_files(LSTM_PATH, XGB_PATH, CONFIG_PATH)
    SCALER_HASH = _sha256_files(FEATURE_SCALER_PATH, TECH_SCALER_PATH, TARGET_STATS_PATH)

RANKING_VERSION = _sha256_files(RANKING_XGB_PATH, RANKING_CALIBRATOR_PATH, RANKING_FEATURES_PATH)

# Serialises LSTM train()/eval() toggle during MC-dropout across concurrent requests.
_model_lock = threading.Lock()

SENTIMENT_WORKERS = int(os.getenv("SENTIMENT_WORKERS", "8"))

# How many names get sentiment each run. 0 = the whole universe, which is the point:
# a panel gathered only for the model's top picks is conditioned on the model's own
# output and cannot answer whether sentiment adds anything (MVP_PLAN § B / § D).
# Non-zero caps the shortlist by predicted strength — an escape hatch for vendor rate
# limits, not the normal setting.
SENTIMENT_TOP_N = int(os.getenv("SENTIMENT_TOP_N", "0"))

# Market data access — vendor code lives behind MarketDataProvider
# (core/data_provider.py). Which market this instance serves is env-driven;
# EGX exists as a disabled scaffold until its licensed data adapter lands.
MARKET = os.getenv("MARKET", "us")
_provider = get_provider(MARKET)
log.info(f"Market data provider: {MARKET} ({type(_provider).__name__})")


# PYDANTIC SCHEMAS

Direction    = Literal["UP", "DOWN"]
SignalLabel  = Literal["POSITIVE", "NEGATIVE", "NEUTRAL"]
Agreement    = Literal["CONFIRMED", "CONTRADICT", "NEUTRAL"]
RiskLevel    = Literal["LOW", "MEDIUM", "HIGH"]


class TickerPrediction(BaseModel):
    ticker:       str
    direction:    Direction   # trees: vs universe median · hybrid: absolute sign
    change_pct:   float       # trees: rel. return vs median % · hybrid: abs. 30d %
    confidence:   float       # trees: calibrated P(beat median) · hybrid: MC-dropout stability
    predicted_at: str
    # Tactical dip-buyer inputs (§ 3.4): oversold state at prediction time.
    rsi_14:        float | None = None
    pct_vs_sma50:  float | None = None
    # Audit snapshot (§ 6.3): the exact scaled inputs + artifact identity.
    features:      dict | None = None
    model_version: str | None = None
    scaler_hash:   str | None = None


class TickerSentiment(BaseModel):
    ticker:               str
    sentiment_score:      float
    signal:               SignalLabel
    analyst_rating:       float | None
    rating_label:         str | None
    ratings_count:        int
    recent_action:        str
    recent_action_firm:   str | None
    recent_actions_count: int
    days_since_latest:    int | None
    pt_current:           float | None
    pt_mean:              float | None
    pt_upside_pct:        float | None
    news_score:           float | None
    news_label:           str | None
    news_count:           int
    components:           dict
    analyzed_at:          str


class ScoreRecord(BaseModel):
    """One record in the /api/score response — matches PredictionRecordDto exactly.
    trees mode: change_pct is relative to the universe median, confidence is the
    calibrated P(beat median); hybrid mode keeps legacy absolute semantics."""
    ticker:           str
    direction:        Direction
    change_pct:       float
    confidence:       float
    sentiment_score:  float
    signal:           SignalLabel
    analyst_rating:   float | None
    rating_label:     str | None
    pt_upside_pct:    float | None
    news_score:       float | None
    agreement:        Agreement
    risk_level:       RiskLevel
    conviction_score: float
    risk_flags:       list[str]
    rationale:        str
    rsi_14:           float | None = None
    pct_vs_sma50:     float | None = None
    # Audit snapshot (§ 6.3) — carried through to the backend for storage.
    features:         dict | None = None
    model_version:    str | None = None
    scaler_hash:      str | None = None


class ScoreResponse(BaseModel):
    generated_at:  str
    count:         int
    records:       list[ScoreRecord]
    # Data-quality gate outcome (§ 6.2). "quarantined" runs are persisted by the
    # backend for audit but never served to the optimizer or users.
    status:        Literal["ok", "quarantined"] = "ok"
    gate_failures: list[str] = []


# LSTM DEFINITION lives in core/lstm.py (shared with training).

# MODEL LOADER — both stacks load so /api/reproduce can replay any snapshot.

def _load_hybrid() -> None:
    log.info("Loading hybrid stack (LSTM + XGBoost head)...")
    lstm = LSTMBackbone(
        input_dim=LSTM_PARAMS["input_dim"],
        hidden_dim=LSTM_PARAMS["hidden_dim"],
        num_layers=LSTM_PARAMS["layers"],
    )
    lstm.load_state_dict(torch.load(LSTM_PATH, map_location="cpu", weights_only=True))
    lstm.eval()

    xgb_model = xgb.XGBRegressor()
    xgb_model.load_model(XGB_PATH)

    with open(FEATURE_SCALER_PATH, "rb") as f:
        feature_scaler = pickle.load(f)
    with open(TECH_SCALER_PATH, "rb") as f:
        tech_scaler = pickle.load(f)

    if TARGET_STATS_PATH.exists():
        with open(TARGET_STATS_PATH) as f:
            target_stats = json.load(f)
        log.info(f"Target stats: mean={target_stats['mean']:.4f}, std={target_stats['std']:.4f}")
    else:
        log.warning("target_stats.json not found — hybrid predictions will not be denormalized.")
        target_stats = {"mean": 0.0, "std": 1.0}

    _state.lstm           = lstm
    _state.xgb_model      = xgb_model
    _state.feature_scaler = feature_scaler
    _state.tech_scaler    = tech_scaler
    _state.target_stats   = target_stats
    log.info("Hybrid stack loaded.")


def _load_ranking() -> None:
    """Ranking champion (MVP_PLAN § A): raw base-14 indicators → XGBoost rank
    score → isotonic P(beat median). No scalers, no torch."""
    log.info("Loading ranking champion (trees-only)...")
    rank_model = xgb.XGBRegressor()
    rank_model.load_model(RANKING_XGB_PATH)
    with open(RANKING_CALIBRATOR_PATH, "rb") as f:
        calibrator = pickle.load(f)
    _state.ranking_model = rank_model
    _state.calibrator    = calibrator
    log.info("Ranking champion loaded.")


def _load_finbert():
    """Load FinBERT text-classification pipeline (CPU). ~440MB, baked into image."""
    try:
        from transformers import pipeline as hf_pipeline
        _state.finbert = hf_pipeline("text-classification", model="ProsusAI/finbert", top_k=None)
        log.info("FinBERT loaded.")
    except Exception as e:
        log.error(f"FinBERT failed to load — news component disabled. ({e})")
        _state.finbert = None


@dataclass
class _AppState:
    lstm:           Any = None
    xgb_model:      Any = None
    feature_scaler: Any = None
    tech_scaler:    Any = None
    target_stats:   dict = field(default_factory=lambda: {"mean": 0.0, "std": 1.0})
    finbert:        Any = None
    # Ranking champion (trees serving mode).
    ranking_model:  Any = None
    calibrator:     Any = None
    ready:          bool = False

_state = _AppState()


# FEATURE ENGINEERING lives in core/features.py (shared with training).

# PREDICTION — serving-mode dispatch

def _infer_trees(tech_raw: np.ndarray) -> tuple[str, float, float]:
    """Trees-only inference core (MVP_PLAN § A): raw base-14 indicator row ->
    (direction, change_pct, confidence).

    change_pct is the expected 21-trading-day return RELATIVE to the universe
    median (the champion's training target); direction is vs-median; confidence
    is the isotonic-calibrated P(beat median) — a real probability, replacing
    MC-dropout pseudo-confidence (IMPLEMENTATION_PLAN §§ 1.1/1.4). Shared by
    live scoring and snapshot replay so audits exercise the exact code path.
    """
    score = float(_state.ranking_model.predict(tech_raw.reshape(1, -1))[0])
    prob  = float(np.clip(_state.calibrator.predict(np.array([score]))[0], 0.0, 1.0))
    direction = "UP" if score > 0 else "DOWN"
    return direction, round(score * 100, 4), round(prob, 4)


def _infer(lstm_window: np.ndarray, tech_last: np.ndarray) -> tuple[str, float, float]:
    """Deterministic inference core: scaled model inputs -> (direction, change_pct, confidence).

    Shared by live scoring and the § 6.3 reproduce endpoint, so an audit exercises
    the *exact* code path that produced the original prediction rather than a
    reimplementation that could silently drift.

    `lstm_window` is [LOOK_BACK, len(FEATURE_COLS)] and `tech_last` is
    [1, len(TECH_COLS)] — both already scaled by the fitted scalers.
    """
    lstm_input = torch.tensor(lstm_window, dtype=torch.float32).unsqueeze(0)

    with torch.no_grad():
        _, hidden = _state.lstm(lstm_input)
    z_pred = float(_state.xgb_model.predict(np.concatenate([hidden.numpy(), tech_last], axis=1))[0])
    raw_pred = z_pred * _state.target_stats["std"] + _state.target_stats["mean"]

    direction  = "UP" if raw_pred > 0 else "DOWN"
    change_pct = round(raw_pred * 100, 4)

    signal_strength = min(abs(z_pred) / Z_REF, 1.0)

    mc = []
    with _model_lock:
        torch.manual_seed(MC_SEED)
        _state.lstm.train()
        try:
            lstm_input_batched = lstm_input.repeat(MC_SAMPLES, 1, 1)   # [30, 60, 5]
            with torch.no_grad():
                _, hiddens = _state.lstm(lstm_input_batched)            # single batched pass
            tech_lasts = np.repeat(tech_last, MC_SAMPLES, axis=0)
            xgb_inputs = np.concatenate([hiddens.numpy(), tech_lasts], axis=1)
            preds_z    = _state.xgb_model.predict(xgb_inputs)
            mc = (preds_z * _state.target_stats["std"] + _state.target_stats["mean"]).tolist()
        finally:
            _state.lstm.eval()
    mc_std = float(np.std(mc)) if mc else 0.0
    if mc_std < 1e-6:
        # MC-dropout is a no-op for single-layer LSTM (dropout=0.0 on 1 layer)
        log.debug("MC samples are identical (single-layer LSTM, dropout inactive). Stability fixed at 1.0.")
    stability = float(np.exp(-mc_std / MC_STD_REF))

    data_quality = float(
        0.5 * (np.abs(lstm_window) <= 1.0).mean()
        + 0.5 * (np.abs(tech_last) <= 1.0).mean()
    )
    confidence = round(float(np.sqrt(signal_strength * stability) * data_quality), 4)

    return direction, change_pct, confidence


def _predict_one(ticker: str, raw: "pd.DataFrame | None" = None) -> TickerPrediction | None:
    """Run LSTM+XGBoost inference for a single ticker. Returns None on any failure."""
    try:
        if raw is None or raw.empty:
            log.warning(f"{ticker}: no price data (likely delisted/renamed). Skipping.")
            return None
        if isinstance(raw.columns, pd.MultiIndex):
            raw.columns = raw.columns.get_level_values(0)
        raw = raw.reset_index()
        raw.columns = [c.lower() for c in raw.columns]
        date_col = [c for c in raw.columns if "date" in c.lower()]
        if not date_col:
            raise KeyError("No date column found after reset_index")
        raw.rename(columns={date_col[0]: "date"}, inplace=True)
        raw["date"] = pd.to_datetime(raw["date"]).dt.strftime("%Y-%m-%d")

        df = raw[["date", "open", "high", "low", "close", "volume"]].copy()
        df.dropna(subset=["close"], inplace=True)
        df.ffill(inplace=True)
        df.dropna(inplace=True)
        df = df.sort_values("date").reset_index(drop=True)

        if len(df) < MIN_RAW_ROWS:
            log.warning(f"{ticker}: only {len(df)} rows, need {MIN_RAW_ROWS}. Skipping.")
            return None

        df = compute_features(df)
        if len(df) < LOOK_BACK:
            log.warning(f"{ticker}: only {len(df)} rows after features, need {LOOK_BACK}. Skipping.")
            return None

        if SERVING_MODEL == "trees":
            # Raw indicator row in the champion's exact feature order (features.json).
            tech_raw = df[RANKING_COLS].iloc[-1].to_numpy(dtype=np.float64)
            direction, change_pct, confidence = _infer_trees(tech_raw)
            features = {
                "v":     SNAPSHOT_SCHEMA,
                "mode":  "trees",
                "tech_last": np.round(tech_raw, SNAPSHOT_DECIMALS).tolist(),
            }
        else:
            feature_scaled = _state.feature_scaler.transform(df[FEATURE_COLS].values)
            tech_scaled    = _state.tech_scaler.transform(df[TECH_COLS].values)

            lstm_window = feature_scaled[-LOOK_BACK:]
            tech_last   = tech_scaled[-1].reshape(1, -1)

            direction, change_pct, confidence = _infer(lstm_window, tech_last)

            # Feature snapshot (§ 6.3): the exact scaled inputs this prediction was
            # made from. Stored so any prediction can be re-run and audited later —
            # impossible to reconstruct after the fact, so it is captured here.
            features = {
                "v":           SNAPSHOT_SCHEMA,
                "mode":        "hybrid",
                "lstm_window": np.round(lstm_window, SNAPSHOT_DECIMALS).tolist(),
                "tech_last":   np.round(tech_last[0], SNAPSHOT_DECIMALS).tolist(),
            }

        # Tactical dip-buyer inputs (§ 3.4): last RSI-14 (already in the feature
        # frame) and distance from the 50-DMA. Best effort — never fails a prediction.
        rsi_14 = pct_vs_sma50 = None
        try:
            rsi_14 = round(float(df["RSI"].iloc[-1]), 2)
            closes = df["close"].astype(float)
            if len(closes) >= 50:
                sma50 = float(closes.rolling(50).mean().iloc[-1])
                if sma50 > 0:
                    pct_vs_sma50 = round(float(closes.iloc[-1]) / sma50 - 1.0, 4)
        except Exception as tac_err:
            log.debug(f"{ticker}: tactical signals unavailable — {tac_err}")

        return TickerPrediction(
            ticker        = ticker,
            direction     = direction,
            change_pct    = change_pct,
            confidence    = confidence,
            predicted_at  = datetime.now(timezone.utc).isoformat(),
            rsi_14        = rsi_14,
            pct_vs_sma50  = pct_vs_sma50,
            features      = features,
            model_version = MODEL_VERSION,
            scaler_hash   = SCALER_HASH,
        )

    except Exception as e:
        log.error(f"{ticker}: prediction failed ΓÇö {e}")
        return None


# SENTIMENT ΓÇö FinBERT + Finnhub + yfinance

FINBERT_MODEL = "ProsusAI/finbert"

# Weights, thresholds and the composite itself live in core.sentiment_scoring so the
# point-in-time replay lane (MVP_PLAN § C) scores through the exact same
# arithmetic. Re-deriving them here is what would make a replayed track record
# measure the replay instead of the strategy.
POS_THRESHOLD = sentiment_scoring.POS_THRESHOLD
NEG_THRESHOLD = sentiment_scoring.NEG_THRESHOLD
PT_REF_PCT    = sentiment_scoring.PT_REF_PCT
WEIGHTS       = sentiment_scoring.WEIGHTS


def _news_sentiment(titles: list[str]):
    """FinBERT over headlines -> (score, label, count). Model call here, arithmetic shared."""
    if not titles or _state.finbert is None:
        return None, None, len(titles) if titles else 0
    try:
        outs = _state.finbert(titles, truncation=True, max_length=128, batch_size=8)
    except Exception as e:
        log.warning(f"FinBERT inference failed: {e}")
        return None, None, len(titles)
    avg = sentiment_scoring.news_score_from_finbert(outs)
    if avg is None:
        return None, None, 0
    return avg, sentiment_scoring.label(avg), len(titles)


def _score_gathered(g: dict) -> TickerSentiment | None:
    """Scoring phase (serial): FinBERT on headlines + weighted composite."""
    try:
        news_score, news_label, news_count = _news_sentiment(g["titles"])

        # Shared with the replay lane, so live and replayed records are scored by the
        # same arithmetic rather than two implementations that agree today.
        score, signal, parts = sentiment_scoring.composite(
            consensus    = sentiment_scoring.consensus_score(g["avg"]),
            actions      = g["action_score"],
            price_target = sentiment_scoring.price_target_score(g["pt_up"]),
            news         = news_score,
        )

        return TickerSentiment(
            ticker               = g["ticker"],
            sentiment_score      = score,
            signal               = signal,
            analyst_rating       = g["avg"],
            rating_label         = g["rating_label"],
            ratings_count        = g["n_analysts"],
            recent_action        = g["latest_action"],
            recent_action_firm   = g["latest_firm"],
            recent_actions_count = g["win_count"],
            days_since_latest    = g["days_since"],
            pt_current           = g["pt_cur"],
            pt_mean              = g["pt_mean"],
            pt_upside_pct        = g["pt_up"],
            news_score           = news_score,
            news_label           = news_label,
            news_count           = news_count,
            components           = parts,
            analyzed_at          = datetime.now(timezone.utc).isoformat(),
        )
    except Exception as e:
        log.error(f"{g.get('ticker')}: scoring failed ΓÇö {e}")
        return None


@asynccontextmanager
async def lifespan(app: FastAPI):
    _load_hybrid()      # always: keeps /api/reproduce able to replay legacy snapshots
    _load_ranking()     # always: trees serving + replay of trees snapshots under any flag
    _load_finbert()
    _state.ready = True
    log.info(f"QuantWise Pipeline service ready (serving_model={SERVING_MODEL}).")
    yield
    log.info("QuantWise Pipeline service shutting down.")


app = FastAPI(
    title="QuantWise Pipeline Service",
    description=(
        "Unified ML pipeline: ticker fetch → prediction → "
        "FinBERT+analyst sentiment → risk rules. "
        f"Serving model: {SERVING_MODEL} (SERVING_MODEL env; trees = ranking champion, "
        "hybrid = legacy LSTM→XGBoost). "
        "Exposes POST /api/score for the .NET Quartz job."
    ),
    version="2.1.0",
    lifespan=lifespan,
)


@app.get("/health")
def health():
    """Liveness check. Returns serving mode + stack status."""
    from markets.us.provider import FINNHUB_API_KEY  # vendor detail, US interim only

    return {
        "status":        "ok",
        "market":        MARKET,
        "serving_model": SERVING_MODEL,
        "model_version": MODEL_VERSION,
        "models":        "loaded" if _state.ready else "not loaded",
        "ranking":       "loaded" if _state.ranking_model is not None else "not loaded",
        "finbert":       "loaded" if _state.finbert is not None else "not loaded (news disabled)",
        "finnhub":       "enabled" if FINNHUB_API_KEY else "disabled (set FINNHUB_API_KEY)",
        # The panel is a compounding asset (MVP_PLAN § B); a gap in it is invisible
        # unless something reports it, and gaps cannot be backfilled past the vendor's
        # ~12-month news horizon. Surface it where uptime checks already look.
        "sentiment_panel": panel_summary(),
        "news_store":      news_summary(),
        "time":          datetime.now(timezone.utc).isoformat(),
    }


class ClosesRequest(BaseModel):
    tickers: list[str]
    start:   str  # ISO date, inclusive
    end:     str  # ISO date, exclusive


class ClosesResponse(BaseModel):
    market: str
    closes: dict[str, dict[str, float]]  # ticker -> {ISO date: adjusted close}


@app.post("/api/closes", response_model=ClosesResponse)
def closes(req: ClosesRequest):
    """Historical closes for realized-outcome scoring (IMPLEMENTATION_PLAN § 0.3).
    Called by the .NET ScoreOutcomesJob to mark matured predictions to market."""
    if not req.tickers:
        raise HTTPException(status_code=400, detail="tickers must be non-empty.")
    if len(req.tickers) > 200:
        raise HTTPException(status_code=400, detail="Too many tickers (max 200).")
    data = _provider.get_closes(req.tickers, req.start, req.end)
    return ClosesResponse(market=MARKET, closes=data)


class InstrumentStatsRequest(BaseModel):
    tickers: list[str] | None = None  # omitted/empty -> current universe


class InstrumentStat(BaseModel):
    ticker: str
    realized_vol_1y: float | None        # annualized std of daily returns
    avg_daily_value_traded: float | None  # 90-day mean of close x volume
    last_close: float | None
    sector: str | None


class InstrumentStatsResponse(BaseModel):
    market: str
    as_of: str
    stats: list[InstrumentStat]


@app.post("/api/instrument-stats", response_model=InstrumentStatsResponse)
def instrument_stats(req: InstrumentStatsRequest):
    """Computed stats for the instrument registry (IMPLEMENTATION_PLAN § 3.1).
    Called nightly by the .NET RefreshInstrumentStatsJob: with no tickers the
    current universe is used, so newly screened names auto-register."""
    tickers = req.tickers or _provider.get_universe()
    if len(tickers) > 300:
        raise HTTPException(status_code=400, detail="Too many tickers (max 300).")

    data = _provider.get_ohlcv_batch(tickers, period="1y")
    try:
        sectors = _provider.get_sector_map(tickers)
    except Exception as e:
        log.warning(f"instrument-stats: sector map unavailable — {e}")
        sectors = {}

    stats: list[InstrumentStat] = []
    is_multi = data is not None and hasattr(data.columns, "levels")
    for t in tickers:
        frame = None
        try:
            if data is not None and not data.empty:
                if is_multi and t in data.columns.get_level_values(0):
                    frame = data[t].dropna(how="all")
                elif not is_multi and t in data.columns:
                    frame = data[t].dropna(how="all")
        except Exception:
            frame = None

        vol = adv = last_close = None
        if frame is not None and len(frame) >= 60 and "Close" in frame:
            closes = frame["Close"].dropna()
            returns = closes.pct_change().dropna()
            if len(returns) >= 60:
                vol = float(returns.std() * np.sqrt(252))
            last_close = float(closes.iloc[-1])
            if "Volume" in frame:
                dv = (frame["Close"] * frame["Volume"]).dropna().tail(90)
                if not dv.empty:
                    adv = float(dv.mean())

        stats.append(InstrumentStat(
            ticker=t,
            realized_vol_1y=vol,
            avg_daily_value_traded=adv,
            last_close=last_close,
            sector=sectors.get(t),
        ))

    computed = sum(1 for s in stats if s.realized_vol_1y is not None)
    log.info(f"instrument-stats: {computed}/{len(tickers)} tickers with stats.")
    return InstrumentStatsResponse(
        market=MARKET,
        as_of=datetime.now(timezone.utc).isoformat(),
        stats=stats,
    )


class ReproduceRequest(BaseModel):
    """A stored § 6.3 snapshot, replayed."""
    features:      dict
    model_version: str | None = None
    scaler_hash:   str | None = None
    # Originally-served values, if the caller wants an equality verdict.
    expected_direction:  str | None = None
    expected_change_pct: float | None = None
    expected_confidence: float | None = None


class ReproduceResponse(BaseModel):
    direction:   str
    change_pct:  float
    confidence:  float
    # Artifact identity now vs at prediction time.
    model_version:         str
    scaler_hash:           str | None   # null when serving trees (no scalers)
    model_version_matches: bool | None = None
    scaler_hash_matches:   bool | None = None
    # Null when the caller supplied nothing to compare against.
    matches:     bool | None = None
    mismatches:  list[str] = []


@app.post("/api/reproduce", response_model=ReproduceResponse)
def reproduce(req: ReproduceRequest):
    """Re-run inference on a stored feature snapshot (IMPLEMENTATION_PLAN § 6.3).

    This is the audit answer and the debugging tool in one: it replays the exact
    inputs through the SAME inference function live scoring uses (`_infer_trees`
    or `_infer`, chosen by the snapshot's "mode"; absent mode = legacy hybrid row)
    so a mismatch means something really did change, not that an audit drifted.

    A differing `model_version` is reported rather than rejected: reproducing an
    old prediction under today's artifacts is exactly how you demonstrate what a
    model change did. Both stacks load at startup, so either snapshot mode is
    replayable under any SERVING_MODEL flag value.
    """
    if not _state.ready:
        raise HTTPException(status_code=503, detail="Models not loaded yet.")

    f = req.features or {}
    if f.get("v") != SNAPSHOT_SCHEMA:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported snapshot schema {f.get('v')!r}; this build reads v{SNAPSHOT_SCHEMA}.")

    mode = f.get("mode", "hybrid")
    if mode == "trees":
        if _state.ranking_model is None:
            raise HTTPException(status_code=503, detail="Ranking artifacts not loaded.")
        try:
            tech_raw = np.asarray(f["tech_last"], dtype=np.float64).ravel()
        except (KeyError, ValueError) as e:
            raise HTTPException(status_code=400, detail=f"Malformed snapshot: {e}")
        if tech_raw.shape[0] != len(RANKING_COLS):
            raise HTTPException(
                status_code=400,
                detail=f"tech_last has {tech_raw.shape[0]} features, expected {len(RANKING_COLS)}.")
        direction, change_pct, confidence = _infer_trees(tech_raw)
    elif mode == "hybrid":
        try:
            lstm_window = np.asarray(f["lstm_window"], dtype=np.float64)
            tech_last   = np.asarray(f["tech_last"], dtype=np.float64).reshape(1, -1)
        except (KeyError, ValueError) as e:
            raise HTTPException(status_code=400, detail=f"Malformed snapshot: {e}")

        expected_shape = (LOOK_BACK, len(FEATURE_COLS))
        if lstm_window.shape != expected_shape:
            raise HTTPException(
                status_code=400,
                detail=f"lstm_window is {lstm_window.shape}, expected {expected_shape}.")
        if tech_last.shape[1] != len(TECH_COLS):
            raise HTTPException(
                status_code=400,
                detail=f"tech_last has {tech_last.shape[1]} features, expected {len(TECH_COLS)}.")
        direction, change_pct, confidence = _infer(lstm_window, tech_last)
    else:
        raise HTTPException(status_code=400, detail=f"Unknown snapshot mode {mode!r}.")

    mismatches: list[str] = []
    compared = False
    if req.expected_direction is not None:
        compared = True
        if req.expected_direction != direction:
            mismatches.append(f"direction: stored {req.expected_direction}, recomputed {direction}")
    if req.expected_change_pct is not None:
        compared = True
        if abs(req.expected_change_pct - change_pct) > 1e-4:
            mismatches.append(f"change_pct: stored {req.expected_change_pct}, recomputed {change_pct}")
    if req.expected_confidence is not None:
        compared = True
        if abs(req.expected_confidence - confidence) > 1e-4:
            mismatches.append(f"confidence: stored {req.expected_confidence}, recomputed {confidence}")

    return ReproduceResponse(
        direction             = direction,
        change_pct            = change_pct,
        confidence            = confidence,
        model_version         = MODEL_VERSION,
        scaler_hash           = SCALER_HASH,
        model_version_matches = None if req.model_version is None else req.model_version == MODEL_VERSION,
        scaler_hash_matches   = None if req.scaler_hash is None else req.scaler_hash == SCALER_HASH,
        matches               = (not mismatches) if compared else None,
        mismatches            = mismatches,
    )


@app.post("/api/score", response_model=ScoreResponse)
def score():
    if not _state.ready:
        raise HTTPException(status_code=503, detail="Models not loaded yet.")

    tickers = _provider.get_universe()
    log.info(f"Scoring {len(tickers)} tickers (serving_model={SERVING_MODEL})...")

    log.info("Batch downloading historical price data...")
    all_data = _provider.get_ohlcv_batch(tickers)

    log.info(f"Running {SERVING_MODEL} predictions...")
    # Pre-build per-ticker frames from the batch download for efficiency
    ticker_frames: dict[str, "pd.DataFrame | None"] = {}
    if all_data is not None and not all_data.empty:
        is_multi = hasattr(all_data.columns, "levels")
        for t in tickers:
            try:
                if is_multi and t in all_data.columns.get_level_values(0):
                    frame = all_data[t].dropna(how="all")
                    ticker_frames[t] = frame if not frame.empty else None
                elif not is_multi and t in all_data.columns:
                    frame = all_data[t].dropna(how="all")
                    ticker_frames[t] = frame if not frame.empty else None
                else:
                    ticker_frames[t] = None
            except Exception as slice_err:
                log.debug(f"{t}: failed to slice batch DataFrame: {slice_err}")
                ticker_frames[t] = None
    predictions: list[TickerPrediction] = []
    for ticker in tickers:
        raw = ticker_frames.get(ticker)
        result = _predict_one(ticker, raw)
        if result:
            predictions.append(result)
        else:
            log.warning(f"{ticker}: prediction skipped.")

    log.info(f"Predictions: {len(predictions)}/{len(tickers)} succeeded.")

    if not predictions:
        raise HTTPException(status_code=500, detail="All predictions failed.")

    # Whole universe by default (MVP_PLAN § B clock 1). The old top-35 shortlist saved
    # FinBERT time but made the accumulating panel a biased sample of the model's own
    # favourites, and no amount of later spend can re-collect the names it skipped.
    if SENTIMENT_TOP_N > 0:
        shortlist = sorted(
            [p for p in predictions if p.change_pct > 0],
            key=lambda x: (x.change_pct, x.confidence),
            reverse=True,
        ) or sorted(predictions, key=lambda x: (x.change_pct, x.confidence), reverse=True)
        predicted_tickers = [p.ticker for p in shortlist[:SENTIMENT_TOP_N]]
        log.warning(
            f"SENTIMENT_TOP_N={SENTIMENT_TOP_N}: gathering sentiment for {len(predicted_tickers)} of "
            f"{len(predictions)} scored names. The daily panel will be biased toward the model's "
            f"own picks for this run.")
    else:
        predicted_tickers = [p.ticker for p in predictions]
        log.info(f"Gathering sentiment for the full universe ({len(predicted_tickers)} names).")

    log.info(f"Running sentiment ({SENTIMENT_WORKERS} workers)...")
    sentiments: list[TickerSentiment] = []

    # Raw headline records are collected as they arrive and archived below. The
    # vendor's retention is rolling, so a headline not stored tonight is gone.
    news_records: list[dict] = []

    with ThreadPoolExecutor(max_workers=SENTIMENT_WORKERS) as ex:
        future_to_ticker = {ex.submit(_provider.gather_ticker_context, t): t for t in predicted_tickers}
        for fut in as_completed(future_to_ticker):
            ticker = future_to_ticker[fut]
            try:
                g = fut.result()
            except Exception:
                g = None
            if g:
                for item in g.get("news_items") or []:
                    row = normalize_news(
                        ticker       = ticker,
                        headline     = item.get("headline"),
                        published_at = item.get("published_at"),
                        source       = item.get("source"),
                        url          = item.get("url"),
                        vendor_id    = item.get("vendor_id"),
                    )
                    if row:
                        news_records.append(row)
            s = _score_gathered(g) if g is not None else None
            if s:
                sentiments.append(s)
            else:
                log.warning(f"{ticker}: sentiment skipped.")

    log.info(f"Sentiment: {len(sentiments)}/{len(predicted_tickers)} succeeded.")

    # News store (§ C): permanent archive of the raw text. Best effort — an archive
    # write must never cost us the run, but a failure here is logged loudly
    # because what is missed tonight cannot be refetched once retention rolls.
    try:
        stats = store_news(news_records)
        log.info(
            f"News store: {stats['written']} new headlines archived "
            f"({stats['duplicates']} already held, {stats['undated']} undated and dropped).")
    except Exception as e:
        log.error(f"News store append FAILED (run continues) — {e}")

    # Append this run to the panel BEFORE risk rules and gates. The panel records what
    # the vendors said today, which stays true and worth keeping even when the run is
    # later quarantined for a price-data problem. Best effort: a panel write must never
    # be able to cost us the run.
    try:
        written = append_daily([s.model_dump() for s in sentiments])
        if written is not None:
            log.info(f"Sentiment panel: appended {len(sentiments)} rows -> {written}")
    except Exception as e:
        log.error(f"Sentiment panel append FAILED (run continues) — {e}")

    log.info("Applying risk rules...")
    pred_dicts = [p.model_dump() for p in predictions]
    sent_dicts = [s.model_dump() for s in sentiments]

    try:
        enriched = apply_risk_rules(pred_dicts, sent_dicts)
    except ValueError as e:
        log.error(str(e))
        raise HTTPException(status_code=500, detail=str(e))

    log.info(f"Risk rules complete: {len(enriched)} records.")

    records = [
        ScoreRecord(
            ticker           = r["ticker"],
            direction        = r["direction"],
            change_pct       = r["change_pct"],
            confidence       = r["confidence"],
            sentiment_score  = r.get("sentiment_score") or 0.0,
            signal           = r.get("signal") or "NEUTRAL",
            analyst_rating   = r.get("analyst_rating"),
            rating_label     = r.get("rating_label"),
            pt_upside_pct    = r.get("pt_upside_pct"),
            news_score       = r.get("news_score"),
            agreement        = r["agreement"],
            risk_level       = r["risk_level"],
            conviction_score = r["conviction_score"],
            risk_flags       = r["risk_flags"],
            rationale        = r["rationale"],
            rsi_14           = r.get("rsi_14"),
            pct_vs_sma50     = r.get("pct_vs_sma50"),
            features         = r.get("features"),
            model_version    = r.get("model_version"),
            scaler_hash      = r.get("scaler_hash"),
        )
        for r in enriched
    ]

    # Data-quality gates (§ 6.2): run between scoring and ingest. A failing run
    # still ships — flagged quarantined so the backend persists it for audit
    # while keeping it invisible to the optimizer and users.
    last_price_date = None
    for frame in ticker_frames.values():
        if frame is not None and len(frame) > 0:
            frame_max = frame.index.max()
            frame_max = frame_max.date() if hasattr(frame_max, "date") else frame_max
            if last_price_date is None or frame_max > last_price_date:
                last_price_date = frame_max

    gate_report = run_quality_gates(
        records=[r.model_dump() for r in records],
        universe_size=len(tickers),
        last_price_date=last_price_date,
        config=_provider.config,
    )
    for check in gate_report.checks:
        log.info(f"quality gate [{'PASS' if check.passed else 'FAIL'}] {check.name}: {check.detail}")
    if not gate_report.passed:
        log.error(f"Quality gates FAILED — run will ship as quarantined: {gate_report.failures}")

    response = ScoreResponse(
        generated_at  = datetime.now(timezone.utc).isoformat(),
        count         = len(records),
        records       = records,
        status        = "ok" if gate_report.passed else "quarantined",
        gate_failures = gate_report.failures,
    )

    # Persist a copy of the exact response JSON beside this file (overwritten each run).
    try:
        out_path = BASE_DIR / "last_score_output.json"
        out_path.write_text(response.model_dump_json(indent=2), encoding="utf-8")
        log.info(f"Wrote score output copy -> {out_path}")
    except Exception as e:
        log.warning(f"Failed to write score output copy: {e}")

    return response
