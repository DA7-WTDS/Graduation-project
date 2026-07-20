# QuantWise — Unified Pipeline Service
# FastAPI service: fetch tickers → LSTM+XGBoost prediction → FinBERT sentiment → risk rules.
# Exposes POST /api/score, called once per day by the .NET FetchDailyPipelineJob Quartz job.
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

with open(CONFIG_PATH) as f:
    CONFIG = json.load(f)

LOOK_BACK    = CONFIG["look_back"]       # 60
FEATURE_COLS = CONFIG["feature_cols"]    # 5 LSTM input features
TECH_COLS    = CONFIG["tech_cols"]       # 14 tech indicator features
LSTM_PARAMS  = CONFIG["lstm_params"]

# Minimum raw OHLCV rows needed before feature engineering:
#   60 (look_back) + 35 (indicator warmup: SMA_30 + momentum lags) + 10 buffer
MIN_RAW_ROWS = 120


MC_SAMPLES   = 30
MC_SEED      = 1234
Z_REF        = 1.0
MC_STD_REF   = 0.015

# Feature-vector snapshotting (§ 6.3). Model identity is content-addressed —
# hashed from the artifacts actually loaded — so it can never drift out of sync
# with a hand-maintained version string. Scalers are hashed separately because a
# scaler swap invalidates stored vectors even when the model is unchanged.
SNAPSHOT_SCHEMA  = 1
SNAPSHOT_DECIMALS = 6  # scaled features live in ~[-3,3]; 6dp reproduces to 4dp output


def _sha256_files(*paths: Path, length: int = 16) -> str:
    h = hashlib.sha256()
    for p in sorted(paths, key=lambda x: x.name):
        try:
            h.update(p.read_bytes())
        except OSError as e:
            log.warning(f"hash: could not read {p.name} — {e}")
            h.update(p.name.encode())
    return h.hexdigest()[:length]


MODEL_VERSION = _sha256_files(LSTM_PATH, XGB_PATH, CONFIG_PATH)
SCALER_HASH   = _sha256_files(FEATURE_SCALER_PATH, TECH_SCALER_PATH, TARGET_STATS_PATH)

# Serialises LSTM train()/eval() toggle during MC-dropout across concurrent requests.
_model_lock = threading.Lock()

SENTIMENT_WORKERS = int(os.getenv("SENTIMENT_WORKERS", "8"))

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
    direction:    Direction
    change_pct:   float
    confidence:   float
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
    """One record in the /api/score response — matches PredictionRecordDto exactly."""
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

# MODEL LOADER

def _load_models():
    log.info("Loading LSTM + XGBoost models...")
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
        log.warning("target_stats.json not found ΓÇö predictions will not be denormalized.")
        target_stats = {"mean": 0.0, "std": 1.0}

    log.info("LSTM + XGBoost loaded.")
    return lstm, xgb_model, feature_scaler, tech_scaler, target_stats


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

_state = _AppState()


# FEATURE ENGINEERING lives in core/features.py (shared with training).

# PREDICTION — LSTM + XGBoost

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

POS_THRESHOLD = 0.15
NEG_THRESHOLD = -0.15
PT_REF_PCT    = 25.0

WEIGHTS = {"consensus": 0.40, "actions": 0.15, "price_target": 0.20, "news": 0.25}

def _news_sentiment(titles: list[str]):
    if not titles or _state.finbert is None:
        return None, None, len(titles) if titles else 0
    try:
        outs = _state.finbert(titles, truncation=True, max_length=128, batch_size=8)
    except Exception as e:
        log.warning(f"FinBERT inference failed: {e}")
        return None, None, len(titles)
    scores = []
    for o in outs:
        d = {x["label"].lower(): x["score"] for x in o}
        scores.append(d.get("positive", 0.0) - d.get("negative", 0.0))
    if not scores:
        return None, None, 0
    avg   = float(np.mean(scores))
    label = "POSITIVE" if avg > POS_THRESHOLD else "NEGATIVE" if avg < NEG_THRESHOLD else "NEUTRAL"
    return round(avg, 3), label, len(titles)


def _score_gathered(g: dict) -> TickerSentiment | None:
    """Scoring phase (serial): FinBERT on headlines + weighted composite."""
    try:
        consensus_score = (g["avg"] - 3.0) / 2.0 if g["avg"] is not None else None
        pt_score        = max(-1.0, min(1.0, g["pt_up"] / PT_REF_PCT)) if g["pt_up"] is not None else None
        news_score, news_label, news_count = _news_sentiment(g["titles"])

        parts = {}
        if consensus_score   is not None: parts["consensus"]    = round(consensus_score, 3)
        if g["action_score"] is not None: parts["actions"]       = g["action_score"]
        if pt_score          is not None: parts["price_target"]  = round(pt_score, 3)
        if news_score        is not None: parts["news"]          = news_score

        if parts:
            wsum  = sum(WEIGHTS[k] for k in parts)
            score = round(sum(parts[k] * WEIGHTS[k] for k in parts) / wsum, 3)
        else:
            score = 0.0

        signal = "POSITIVE" if score > POS_THRESHOLD else "NEGATIVE" if score < NEG_THRESHOLD else "NEUTRAL"

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
    lstm, xgb_model, feature_scaler, tech_scaler, target_stats = _load_models()
    _state.lstm           = lstm
    _state.xgb_model      = xgb_model
    _state.feature_scaler = feature_scaler
    _state.tech_scaler    = tech_scaler
    _state.target_stats   = target_stats
    _load_finbert()
    log.info("QuantWise Pipeline service ready.")
    yield
    log.info("QuantWise Pipeline service shutting down.")


app = FastAPI(
    title="QuantWise Pipeline Service",
    description=(
        "Unified ML pipeline: ticker fetch ΓåÆ LSTM+XGBoost prediction ΓåÆ "
        "FinBERT+analyst sentiment ΓåÆ risk rules. "
        "Exposes POST /api/score for the .NET Quartz job."
    ),
    version="2.0.0",
    lifespan=lifespan,
)


@app.get("/health")
def health():
    """Liveness check. Returns model + FinBERT status."""
    from markets.us.provider import FINNHUB_API_KEY  # vendor detail, US interim only

    return {
        "status":   "ok",
        "market":   MARKET,
        "models":   "loaded" if _state.lstm is not None else "not loaded",
        "finbert":  "loaded" if _state.finbert is not None else "not loaded (news disabled)",
        "finnhub":  "enabled" if FINNHUB_API_KEY else "disabled (set FINNHUB_API_KEY)",
        "time":     datetime.now(timezone.utc).isoformat(),
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
    scaler_hash:           str
    model_version_matches: bool | None = None
    scaler_hash_matches:   bool | None = None
    # Null when the caller supplied nothing to compare against.
    matches:     bool | None = None
    mismatches:  list[str] = []


@app.post("/api/reproduce", response_model=ReproduceResponse)
def reproduce(req: ReproduceRequest):
    """Re-run inference on a stored feature snapshot (IMPLEMENTATION_PLAN § 6.3).

    This is the audit answer and the debugging tool in one: it replays the exact
    scaled inputs through `_infer` — the same function live scoring uses — so a
    mismatch means something really did change, not that an audit path drifted.

    A differing `model_version` is reported rather than rejected: reproducing an
    old prediction under today's artifacts is exactly how you demonstrate what a
    model change did.
    """
    if _state.lstm is None:
        raise HTTPException(status_code=503, detail="Models not loaded yet.")

    f = req.features or {}
    if f.get("v") != SNAPSHOT_SCHEMA:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported snapshot schema {f.get('v')!r}; this build reads v{SNAPSHOT_SCHEMA}.")

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
    if _state.lstm is None:
        raise HTTPException(status_code=503, detail="Models not loaded yet.")

    tickers = _provider.get_universe()
    log.info(f"Scoring {len(tickers)} tickers...")

    log.info("Batch downloading historical price data...")
    all_data = _provider.get_ohlcv_batch(tickers)

    log.info("Running LSTM+XGBoost predictions...")
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

    # Run FinBERT only on top 35 candidates by projected return (skips ~65% of inference)
    sorted_preds = sorted(
        [p for p in predictions if p.change_pct > 0],
        key=lambda x: (x.change_pct, x.confidence),
        reverse=True,
    )
    if len(sorted_preds) < 15:  # fallback: use top 35 overall if few positive predictions
        sorted_preds = sorted(
            predictions,
            key=lambda x: (x.change_pct, x.confidence),
            reverse=True,
        )
    top_candidates    = sorted_preds[:35]
    predicted_tickers = [p.ticker for p in top_candidates]
    log.info(f"Selected top {len(predicted_tickers)} candidates for sentiment analysis.")

    log.info(f"Running sentiment ({SENTIMENT_WORKERS} workers)...")
    sentiments: list[TickerSentiment] = []

    with ThreadPoolExecutor(max_workers=SENTIMENT_WORKERS) as ex:
        future_to_ticker = {ex.submit(_provider.gather_ticker_context, t): t for t in predicted_tickers}
        for fut in as_completed(future_to_ticker):
            ticker = future_to_ticker[fut]
            try:
                g = fut.result()
            except Exception:
                g = None
            s = _score_gathered(g) if g is not None else None
            if s:
                sentiments.append(s)
            else:
                log.warning(f"{ticker}: sentiment skipped.")

    log.info(f"Sentiment: {len(sentiments)}/{len(predicted_tickers)} succeeded.")

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
