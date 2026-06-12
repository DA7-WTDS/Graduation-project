"""
QuantWise ΓÇö Unified Pipeline Service

Single FastAPI service that chains the full daily scoring pipeline:
  1. Fetch top ~100 US large-cap tickers
  2. Predict 30-day return (hybrid LSTM + XGBoost)
  3. Score sentiment (analyst consensus + FinBERT news)
  4. Apply risk rules (merge + enrich)
  5. Expose POST /api/score for the .NET Quartz job to consume

The .NET backend (FetchDailyPipelineJob) calls POST /api/score once per day.
No n8n, no scheduling inside Python ΓÇö the .NET Quartz job is the scheduler.

Ports:
  8000 ΓÇö this service

CRITICAL: torch.backends.mkldnn.enabled = False
  nn.LSTM's oneDNN/MKLDNN CPU kernel returns nondeterministic garbage when
  executed off the main thread (uvicorn serves sync endpoints from a worker
  thread). Disabling MKLDNN forces the native, thread-safe RNN path.
  Keep this line ΓÇö removing it produces wildly unstable predictions.
"""

import json
import logging
import os
import pickle
import re
import threading
import time
import warnings
from concurrent.futures import ThreadPoolExecutor, as_completed
from contextlib import asynccontextmanager
from datetime import datetime, timedelta
from pathlib import Path

import numpy as np
import pandas as pd
import requests
import torch
import torch.nn as nn
import xgboost as xgb
import yfinance as yf
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, field_validator

from risk_rules import apply_risk_rules

warnings.filterwarnings("ignore")

# ΓöÇΓöÇ MKLDNN fix (see module docstring) ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
torch.backends.mkldnn.enabled = False

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
log = logging.getLogger(__name__)


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# CONFIG ΓÇö Paths
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

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

# yfinance fetch settings
FETCH_PERIOD     = "6mo"
FETCH_INTERVAL   = "1d"
FETCH_BATCH_SIZE = 10
FETCH_MIN_ROWS   = 80


# ΓöÇΓöÇ Confidence metric tuning ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
MC_SAMPLES   = 30
MC_SEED      = 1234
Z_REF        = 1.0
MC_STD_REF   = 0.015

# Serialises LSTM train()/eval() toggle during MC-dropout across concurrent requests.
_model_lock = threading.Lock()


# ΓöÇΓöÇ yfinance hardening ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
yf.config.network.retries = int(os.getenv("YF_RETRIES", "3"))
_YF_PROXY = os.getenv("YF_PROXY") or os.getenv("HTTPS_PROXY")
if _YF_PROXY:
    yf.config.network.proxy = _YF_PROXY
    log.info("yfinance proxy enabled.")
_YF_CACHE_DIR = os.getenv("YF_CACHE_DIR")
if _YF_CACHE_DIR:
    try:
        yf.set_tz_cache_location(_YF_CACHE_DIR)
        log.info(f"yfinance tz/cookie cache ΓåÆ {_YF_CACHE_DIR}")
    except Exception as e:
        log.warning(f"Could not set yfinance cache location: {e}")

YF_MIN_INTERVAL   = float(os.getenv("YF_MIN_INTERVAL", "0.3"))
_yf_throttle_lock = threading.Lock()
_yf_last_call     = [0.0]

def _yf_throttle():
    """Space out Yahoo calls so bursts don't trip rate limits / IP bans."""
    if YF_MIN_INTERVAL <= 0:
        return
    with _yf_throttle_lock:
        wait = YF_MIN_INTERVAL - (time.time() - _yf_last_call[0])
        if wait > 0:
            time.sleep(wait)
        _yf_last_call[0] = time.time()


# ΓöÇΓöÇ Finnhub (sentiment) ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
FINNHUB_API_KEY   = os.getenv("FINNHUB_API_KEY", "").strip()
FINNHUB_BASE      = "https://finnhub.io/api/v1"
FINNHUB_NEWS_DAYS = 14
FINNHUB_FETCH_MAX = 150

SENTIMENT_WORKERS    = int(os.getenv("SENTIMENT_WORKERS", "8"))
FINNHUB_MIN_INTERVAL = 1.05  # stays under 60/min free limit

_finnhub_lock = threading.Lock()
_finnhub_last = [0.0]

def _finnhub_throttle():
    with _finnhub_lock:
        wait = FINNHUB_MIN_INTERVAL - (time.time() - _finnhub_last[0])
        if wait > 0:
            time.sleep(wait)
        _finnhub_last[0] = time.time()


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# PYDANTIC SCHEMAS
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

class TickerPrediction(BaseModel):
    ticker:       str
    direction:    str
    change_pct:   float
    confidence:   float
    predicted_at: str


class TickerSentiment(BaseModel):
    ticker:               str
    sentiment_score:      float
    signal:               str
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
    """One record in the /api/score response ΓÇö matches PredictionRecordDto exactly."""
    ticker:           str
    direction:        str
    change_pct:       float
    confidence:       float
    sentiment_score:  float
    signal:           str
    analyst_rating:   float | None
    rating_label:     str | None
    pt_upside_pct:    float | None
    news_score:       float | None
    agreement:        str
    risk_level:       str
    conviction_score: float
    risk_flags:       list[str]
    rationale:        str


class ScoreResponse(BaseModel):
    generated_at: str
    count:        int
    records:      list[ScoreRecord]


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# LSTM DEFINITION  (must match training notebook exactly)
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

class LSTMBackbone(nn.Module):
    def __init__(self, input_dim: int, hidden_dim: int, num_layers: int):
        super().__init__()
        self.hidden_dim = hidden_dim
        self.num_layers = num_layers
        self.lstm = nn.LSTM(
            input_size=input_dim,
            hidden_size=hidden_dim,
            num_layers=num_layers,
            batch_first=True,
            dropout=0.5 if num_layers > 1 else 0.0,
        )
        self.fc = nn.Linear(hidden_dim, 1)

    def forward(self, x: torch.Tensor):
        h0 = torch.zeros(self.num_layers, x.size(0), self.hidden_dim)
        c0 = torch.zeros(self.num_layers, x.size(0), self.hidden_dim)
        out, _ = self.lstm(x, (h0, c0))
        features   = out[:, -1, :]   # last time step ΓÇö matches notebook (NOT h_n[-1])
        prediction = self.fc(features)
        return prediction, features


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# MODEL LOADER
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

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
    global _finbert
    try:
        from transformers import pipeline as hf_pipeline
        _finbert = hf_pipeline("text-classification", model="ProsusAI/finbert", top_k=None)
        log.info("FinBERT loaded.")
    except Exception as e:
        log.error(f"FinBERT failed to load ΓÇö news component disabled. ({e})")
        _finbert = None


# ΓöÇΓöÇ App-global model state ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
_lstm           = None
_xgb_model      = None
_feature_scaler = None
_tech_scaler    = None
_target_stats   = None
_finbert        = None


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# FEATURE ENGINEERING  (must match training notebook exactly)
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

def _compute_rsi(series: pd.Series, period: int = 14) -> pd.Series:
    delta    = series.diff()
    gain     = delta.where(delta > 0, 0.0)
    loss     = (-delta).where(delta < 0, 0.0)
    avg_gain = gain.rolling(period).mean()
    avg_loss = loss.rolling(period).mean()
    rs       = avg_gain / avg_loss
    return 100.0 - (100.0 / (1.0 + rs))


def _compute_features(df: pd.DataFrame) -> pd.DataFrame:
    df = df.copy()
    close  = df["close"]
    volume = df["volume"]

    df["Return"]       = close.pct_change()
    vol_sma20          = volume.rolling(20).mean()
    df["Volume_Ratio"] = volume / vol_sma20
    df["RSI"]          = _compute_rsi(close, period=14)

    ema_12             = close.ewm(span=12, min_periods=12).mean()
    ema_26             = close.ewm(span=26, min_periods=26).mean()
    df["MACD"]         = (ema_12 - ema_26) / close
    df["MACD_signal"]  = df["MACD"].ewm(span=9, min_periods=9).mean()
    df["MACD_hist"]    = df["MACD"] - df["MACD_signal"]

    for window in [5, 10, 15, 30]:
        df[f"SMA_{window}_Ratio"] = (close.rolling(window).mean().shift() / close).fillna(1.0)
    df["EMA_9_Ratio"]   = (close.ewm(span=9).mean().shift() / close).fillna(1.0)
    df["Volatility_20"] = close.pct_change().rolling(20).std()
    df["Momentum_10"]   = close.pct_change(periods=10)
    df["Momentum_21"]   = close.pct_change(periods=21)
    df["Volume_Change"] = volume.pct_change()
    df["Volume_Ratio"]  = df["Volume_Ratio"].replace([np.inf, -np.inf], np.nan)

    df.dropna(inplace=True)
    df.reset_index(drop=True, inplace=True)
    return df


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# TICKER UNIVERSE
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

_FOREIGN_ADR_DENYLIST = {
    "HSBC", "AZN", "NVS", "SHEL", "BHP", "RIO", "TTE", "BUD", "UBS",
    "BP", "DEO", "BTI", "GSK", "SAP", "SNY", "UL", "NGG", "E", "TEF",
    "TSM", "BABA", "ASML", "NVO", "SONY", "TM", "PDD", "SPOT",
}

_FALLBACK_TICKERS = [
    "AAPL", "NVDA", "MSFT", "AMZN", "GOOGL", "GOOG", "META", "TSLA",
    "AVGO", "TSM", "BRK-B", "JPM", "V", "MA", "BAC", "WFC", "GS", "MS",
    "AXP", "BLK", "LLY", "UNH", "JNJ", "ABBV", "MRK", "TMO", "ABT", "DHR",
    "ISRG", "PFE", "WMT", "COST", "PG", "KO", "PEP", "MCD", "NKE", "SBUX",
    "TGT", "HD", "XOM", "CVX", "COP", "SLB", "EOG", "MPC", "PSX", "VLO",
    "OXY", "HAL", "CAT", "DE", "HON", "UPS", "RTX", "LMT", "GE", "MMM",
    "BA", "FDX", "AMD", "INTC", "QCOM", "TXN", "MU", "AMAT", "LRCX", "KLAC",
    "MRVL", "ARM", "CRM", "ORCL", "NOW", "ADBE", "INTU", "PANW", "SNOW",
    "PLTR", "UBER", "ABNB", "NFLX", "DIS", "CMCSA", "T", "VZ", "TMUS",
    "CHTR", "WBD", "PSKY", "FOX", "PYPL", "XYZ", "SHOP", "COIN", "MSTR",
    "AMT", "PLD", "SPG", "O", "WELL",
]


def _get_top_100_tickers() -> list[str]:
    """
    Returns the top 100 US large-cap tickers via EquityQuery screener.
    Falls back to hardcoded list if screener fails or returns suspicious results.
    """
    log.info("Fetching top 100 tickers via EquityQuery screener...")
    try:
        from yfinance import EquityQuery, screen

        q = EquityQuery("and", [
            EquityQuery("eq",    ["region", "us"]),
            EquityQuery("is-in", ["exchange", "NMS", "NYQ"]),
            EquityQuery("gt",    ["intradaymarketcap", 10_000_000_000]),
        ])
        result = screen(q, sortField="intradaymarketcap", sortAsc=False, size=200)

        raw_tickers = []
        for item in result.get("quotes", []):
            sym = item.get("symbol")
            if not sym or "-" in sym:
                continue
            if item.get("financialCurrency") not in ("USD", None):
                continue
            if sym in _FOREIGN_ADR_DENYLIST:
                continue
            raw_tickers.append(sym)
        raw_tickers = raw_tickers[:100]

        fallback_set = set(_FALLBACK_TICKERS)
        known_us = sum(1 for t in raw_tickers if t in fallback_set)
        overlap_pct = known_us / len(raw_tickers) if raw_tickers else 0

        if len(raw_tickers) >= 80 and overlap_pct >= 0.6:
            log.info(f"Screener: {len(raw_tickers)} tickers ({overlap_pct:.0%} overlap).")
            return raw_tickers
        else:
            log.warning(f"Screener suspicious ({len(raw_tickers)} tickers, {overlap_pct:.0%} overlap). Falling back.")
            return _FALLBACK_TICKERS

    except Exception as e:
        log.warning(f"EquityQuery screener failed ({e}), using fallback list.")
        return _FALLBACK_TICKERS


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# PREDICTION ΓÇö LSTM + XGBoost
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

def _predict_one(ticker: str, raw: "pd.DataFrame | None" = None) -> TickerPrediction | None:
    """Run LSTM+XGBoost inference for a single ticker.

    Parameters
    ----------
    ticker : str
        The stock ticker symbol.
    raw : pd.DataFrame | None
        Pre-downloaded OHLCV data supplied by the caller's bulk batch
        download. If None, the function returns None immediately.

    Returns None on any failure.
    """
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

        df = _compute_features(df)
        if len(df) < LOOK_BACK:
            log.warning(f"{ticker}: only {len(df)} rows after features, need {LOOK_BACK}. Skipping.")
            return None

        feature_scaled = _feature_scaler.transform(df[FEATURE_COLS].values)
        tech_scaled    = _tech_scaler.transform(df[TECH_COLS].values)

        lstm_window = feature_scaled[-LOOK_BACK:]
        lstm_input  = torch.tensor(lstm_window, dtype=torch.float32).unsqueeze(0)
        tech_last   = tech_scaled[-1].reshape(1, -1)

        def _z_from(hidden_np):
            return float(_xgb_model.predict(np.concatenate([hidden_np, tech_last], axis=1))[0])

        with torch.no_grad():
            _, hidden = _lstm(lstm_input)
        z_pred   = _z_from(hidden.numpy())
        raw_pred = z_pred * _target_stats["std"] + _target_stats["mean"]

        direction  = "UP" if raw_pred > 0 else "DOWN"
        change_pct = round(raw_pred * 100, 4)

        signal_strength = min(abs(z_pred) / Z_REF, 1.0)

        # -- Vectorized MC-Dropout ----------------------------------------
        # Replicate the input tensor MC_SAMPLES times along the batch dim
        # and run all 30 stochastic passes in a single batched forward call.
        # This reduces MC-dropout compute time by ~95% vs. a for-loop.
        mc = []
        with _model_lock:
            torch.manual_seed(MC_SEED)
            _lstm.train()
            try:
                lstm_input_batched = lstm_input.repeat(MC_SAMPLES, 1, 1)   # [30, 60, 5]
                with torch.no_grad():
                    _, hiddens = _lstm(lstm_input_batched)                  # single batched pass
                tech_lasts = np.repeat(tech_last, MC_SAMPLES, axis=0)
                xgb_inputs = np.concatenate([hiddens.numpy(), tech_lasts], axis=1)
                preds_z    = _xgb_model.predict(xgb_inputs)
                mc = (preds_z * _target_stats["std"] + _target_stats["mean"]).tolist()
            finally:
                _lstm.eval()
        mc_std    = float(np.std(mc)) if mc else 0.0
        stability = float(np.exp(-mc_std / MC_STD_REF))

        data_quality = float(
            0.5 * (np.abs(lstm_window) <= 1.0).mean()
            + 0.5 * (np.abs(tech_last) <= 1.0).mean()
        )
        confidence = round(float(np.sqrt(signal_strength * stability) * data_quality), 4)

        return TickerPrediction(
            ticker       = ticker,
            direction    = direction,
            change_pct   = change_pct,
            confidence   = confidence,
            predicted_at = datetime.utcnow().isoformat(),
        )

    except Exception as e:
        log.error(f"{ticker}: prediction failed ΓÇö {e}")
        return None


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# SENTIMENT ΓÇö FinBERT + Finnhub + yfinance
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

SENTIMENT_WINDOW_DAYS = 30
NEWS_LIMIT            = 25
NEWS_MIN_RELEVANT     = 3
FINBERT_MODEL         = "ProsusAI/finbert"

POS_THRESHOLD = 0.15
NEG_THRESHOLD = -0.15
PT_REF_PCT    = 25.0

WEIGHTS = {"consensus": 0.40, "actions": 0.15, "price_target": 0.20, "news": 0.25}

_NAME_STOPWORDS = {
    "the", "inc", "co", "corp", "corporation", "company", "ltd",
    "plc", "group", "holdings", "com", "class", "incorporated", "llc", "sa", "nv", "ag", "and",
}
_GRADE_MAP = {
    "strong buy": 1.0, "conviction buy": 1.0, "buy": 0.6, "outperform": 0.6,
    "overweight": 0.6, "accumulate": 0.5, "add": 0.5, "positive": 0.6,
    "market outperform": 0.6, "sector outperform": 0.6, "long-term buy": 0.5,
    "hold": 0.0, "neutral": 0.0, "equal-weight": 0.0, "equalweight": 0.0,
    "market perform": 0.0, "sector perform": 0.0, "in-line": 0.0, "peer perform": 0.0,
    "reduce": -0.5, "sell": -0.6, "underperform": -0.6, "underweight": -0.6,
    "negative": -0.6, "market underperform": -0.6, "sector underperform": -0.6,
    "strong sell": -1.0,
}
_ACTION_LABEL = {"up": "upgrade", "down": "downgrade", "init": "initiated",
                 "main": "maintained", "reit": "reiterated"}


def _finnhub_get(path: str, params: dict):
    if not FINNHUB_API_KEY:
        return None
    try:
        _finnhub_throttle()
        resp = requests.get(
            f"{FINNHUB_BASE}{path}",
            params={**params, "token": FINNHUB_API_KEY},
            timeout=15,
        )
        if resp.status_code != 200:
            log.warning(f"Finnhub {path} HTTP {resp.status_code}")
            return None
        return resp.json()
    except Exception as e:
        log.warning(f"Finnhub {path} failed ΓÇö {e}")
        return None


def _rating_label(avg: float) -> str:
    if avg >= 4.5: return "Strong Buy"
    if avg >= 3.5: return "Buy"
    if avg >= 2.5: return "Hold"
    if avg >= 1.5: return "Sell"
    return "Strong Sell"


def _finnhub_recommendation(ticker: str):
    data = _finnhub_get("/stock/recommendation", {"symbol": ticker})
    if not isinstance(data, list) or not data:
        return None
    row = data[0]
    sb = float(row.get("strongBuy") or 0); b  = float(row.get("buy") or 0)
    h  = float(row.get("hold") or 0);      s  = float(row.get("sell") or 0)
    ss = float(row.get("strongSell") or 0)
    n  = sb + b + h + s + ss
    if n <= 0:
        return None
    avg = (5 * sb + 4 * b + 3 * h + 2 * s + 1 * ss) / n
    return round(avg, 2), _rating_label(avg), int(n)


def _finnhub_profile_name(ticker: str) -> str:
    data = _finnhub_get("/stock/profile2", {"symbol": ticker})
    return (data.get("name") or "") if isinstance(data, dict) else ""


def _find_col(cols, name: str):
    target = name.lower().replace(" ", "")
    for c in cols:
        if str(c).lower().replace(" ", "") == target:
            return c
    return None


def _consensus(tk, ticker: str) -> tuple[float | None, str | None, int]:
    fh = _finnhub_recommendation(ticker)
    if fh is not None:
        return fh
    rec = None
    try:
        _yf_throttle(); rec = tk.get_recommendations()
    except Exception:
        try: rec = tk.recommendations
        except Exception: rec = None
    if rec is not None and len(rec):
        cols = list(rec.columns)
        if _find_col(cols, "strongBuy") is not None:
            row = rec.iloc[-1]
            pc  = _find_col(cols, "period")
            if pc is not None and (rec[pc] == "0m").any():
                row = rec[rec[pc] == "0m"].iloc[-1]
            def g(n):
                c = _find_col(cols, n)
                return float(row[c]) if c is not None and pd.notna(row[c]) else 0.0
            sb, b, h, s, ss = g("strongBuy"), g("buy"), g("hold"), g("sell"), g("strongSell")
            n = sb + b + h + s + ss
            if n > 0:
                avg = (5 * sb + 4 * b + 3 * h + 2 * s + 1 * ss) / n
                return round(avg, 2), _rating_label(avg), int(n)
    return None, None, 0


def _recent_actions(tk, now):
    try:
        _yf_throttle(); ud = tk.get_upgrades_downgrades()
    except Exception:
        ud = None
    if ud is None or len(ud) == 0:
        return None, "none", None, 0, None
    ud = ud.reset_index()
    ud.columns = [str(c).lower().replace(" ", "") for c in ud.columns]
    dcol = _find_col(ud.columns, "gradedate") or _find_col(ud.columns, "date") or ud.columns[0]
    try:
        ud[dcol] = pd.to_datetime(ud[dcol], utc=True).dt.tz_localize(None)
    except Exception:
        ud[dcol] = pd.to_datetime(ud[dcol], errors="coerce")
    ud = ud.dropna(subset=[dcol]).sort_values(dcol)
    if ud.empty:
        return None, "none", None, 0, None
    latest       = ud.iloc[-1]
    latest_action = _ACTION_LABEL.get(str(latest.get("action", "")).lower(), str(latest.get("action", "")) or "none")
    latest_firm   = latest.get("firm") if "firm" in ud.columns else None
    days_since    = int((now - latest[dcol]).days)
    cutoff = now - pd.Timedelta(days=SENTIMENT_WINDOW_DAYS)
    recent = ud[ud[dcol] >= cutoff]
    if recent.empty:
        return None, latest_action, (latest_firm or None), 0, days_since
    num = den = 0.0
    for _, r in recent.iterrows():
        days_ago = max(0, (now - r[dcol]).days)
        w        = max(0.1, 1.0 - days_ago / SENTIMENT_WINDOW_DAYS)
        act      = str(r.get("action", "")).lower()
        act_dir  = 1.0 if act == "up" else -1.0 if act == "down" else 0.0
        grade    = _GRADE_MAP.get(str(r.get("tograde", "")).lower())
        row_score = act_dir if grade is None else 0.5 * act_dir + 0.5 * grade
        num += w * row_score
        den += w
    action_score = round(num / den, 3) if den else None
    return action_score, latest_action, (latest_firm or None), int(len(recent)), days_since


def _price_targets(tk):
    try:
        _yf_throttle()
        pt = tk.get_analyst_price_targets()
        if isinstance(pt, dict):
            cur, mean = pt.get("current"), pt.get("mean")
            if cur and mean and float(cur) > 0:
                up = (float(mean) - float(cur)) / float(cur) * 100
                return float(cur), float(mean), round(up, 2)
    except Exception:
        pass
    return None, None, None


def _company_keywords(ticker: str, name: str):
    pats = []
    t = ticker.strip()
    if len(t) >= 2:
        pats.append(re.compile(r"\b" + re.escape(t) + r"\b"))
    name = name or ""
    for w in re.split(r"[^A-Za-z0-9&]+", name):
        wl = w.strip().lower()
        if len(wl) >= 4 and wl not in _NAME_STOPWORDS:
            pats.append(re.compile(re.escape(wl), re.IGNORECASE))
            break
    return pats


def _filter_relevant(titles: list[str], pats) -> list[str]:
    if not pats:
        return []
    return [t for t in titles if any(p.search(t) for p in pats)]


def _finnhub_raw_headlines(ticker: str):
    if not FINNHUB_API_KEY:
        return None
    try:
        _finnhub_throttle()
        to_d   = datetime.utcnow().date()
        from_d = to_d - timedelta(days=FINNHUB_NEWS_DAYS)
        resp = requests.get(
            f"{FINNHUB_BASE}/company-news",
            params={"symbol": ticker, "from": from_d.isoformat(), "to": to_d.isoformat(), "token": FINNHUB_API_KEY},
            timeout=15,
        )
        if resp.status_code != 200:
            return None
        data = resp.json()
        if not isinstance(data, list):
            return None
        items  = sorted(data, key=lambda x: x.get("datetime", 0), reverse=True)
        titles, seen = [], set()
        for it in items:
            h = (it.get("headline") or "").strip()
            if h and h.lower() not in seen:
                seen.add(h.lower()); titles.append(h)
            if len(titles) >= FINNHUB_FETCH_MAX:
                break
        return titles
    except Exception as e:
        log.warning(f"{ticker}: Finnhub news failed ΓÇö {e}")
        return None


def _yfinance_raw_headlines(tk) -> list[str]:
    try:
        _yf_throttle(); news = tk.news
    except Exception:
        return []
    titles = []
    for item in (news or []):
        if not isinstance(item, dict): continue
        title   = None
        content = item.get("content")
        if isinstance(content, dict): title = content.get("title")
        title = title or item.get("title")
        if title and isinstance(title, str): titles.append(title.strip())
    return titles


def _news_titles_for(tk, ticker: str, name: str) -> list[str]:
    raw = _finnhub_raw_headlines(ticker)
    if raw is None:
        raw = _yfinance_raw_headlines(tk)
    relevant = _filter_relevant(raw, _company_keywords(ticker, name))
    if len(relevant) < NEWS_MIN_RELEVANT:
        return []
    return relevant[:NEWS_LIMIT]


def _news_sentiment(titles: list[str]):
    if not titles or _finbert is None:
        return None, None, len(titles) if titles else 0
    try:
        outs = _finbert(titles, truncation=True, max_length=128, batch_size=8)
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


def _gather_ticker(ticker: str) -> dict | None:
    """Network I/O phase (runs in thread pool): fetch analyst data + news headlines."""
    try:
        tk  = yf.Ticker(ticker)
        now = pd.Timestamp(datetime.utcnow())
        name = _finnhub_profile_name(ticker)
        avg, rating_label, n_analysts = _consensus(tk, ticker)
        action_score, latest_action, latest_firm, win_count, days_since = _recent_actions(tk, now)
        pt_cur, pt_mean, pt_up = _price_targets(tk)
        titles = _news_titles_for(tk, ticker, name)
        return {
            "ticker": ticker, "avg": avg, "rating_label": rating_label, "n_analysts": n_analysts,
            "action_score": action_score, "latest_action": latest_action, "latest_firm": latest_firm,
            "win_count": win_count, "days_since": days_since,
            "pt_cur": pt_cur, "pt_mean": pt_mean, "pt_up": pt_up,
            "titles": titles,
        }
    except Exception as e:
        log.error(f"{ticker}: gather failed ΓÇö {e}")
        return None


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
            analyzed_at          = datetime.utcnow().isoformat(),
        )
    except Exception as e:
        log.error(f"{g.get('ticker')}: scoring failed ΓÇö {e}")
        return None


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# APP STARTUP
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

@asynccontextmanager
async def lifespan(app: FastAPI):
    global _lstm, _xgb_model, _feature_scaler, _tech_scaler, _target_stats
    _lstm, _xgb_model, _feature_scaler, _tech_scaler, _target_stats = _load_models()
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


# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
# ENDPOINTS
# ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

@app.get("/health")
def health():
    """Liveness check. Returns model + FinBERT status."""
    return {
        "status":   "ok",
        "models":   "loaded" if _lstm is not None else "not loaded",
        "finbert":  "loaded" if _finbert is not None else "not loaded (news disabled)",
        "finnhub":  "enabled" if FINNHUB_API_KEY else "disabled (set FINNHUB_API_KEY)",
        "time":     datetime.utcnow().isoformat(),
    }


@app.post("/api/score", response_model=ScoreResponse)
def score():
    """
    Run the full daily scoring pipeline and return risk-graded stock records.

    Called once per day by the .NET FetchDailyPipelineJob Quartz job.
    No authentication required ΓÇö this endpoint is not internet-exposed;
    it runs on localhost and the .NET Quartz job calls it over the same machine.

    Flow:
      1.   Fetch top ~100 US large-cap tickers
      1.5  Batch download historical data (single yfinance round-trip)
      2.   LSTM + XGBoost prediction with vectorized MC-Dropout
      3.   Targeted FinBERT sentiment on top 35 candidates only
      4.   Risk rules merge + enrich
      5.   Return results (requires >= 25 records)
    """
    if _lstm is None:
        raise HTTPException(status_code=503, detail="Models not loaded yet.")

    # ΓöÇΓöÇ 1. Ticker universe ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
    tickers = _get_top_100_tickers()
    log.info(f"Scoring {len(tickers)} tickers...")

    # ΓöÇΓöÇ 2. Predictions ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
    # -- 1.5 Batch download historical data ------------------------------
    # One yfinance round-trip for all 100 tickers instead of 100 separate
    # calls. Reduces historical data fetch latency from ~30 s to ~1.5 s.
    log.info("Batch downloading historical price data from yfinance...")
    all_data = None
    try:
        all_data = yf.download(
            tickers=tickers,
            period=FETCH_PERIOD,
            interval=FETCH_INTERVAL,
            auto_adjust=True,
            progress=False,
            group_by="ticker",
        )
    except Exception as e:
        log.error(f"Batch historical data download failed: {e}")

    log.info("Running LSTM+XGBoost predictions...")
    predictions: list[TickerPrediction] = []
    for ticker in tickers:
        raw = None
        if all_data is not None and not all_data.empty:
            try:
                # Multi-ticker batch download returns MultiIndex columns [metric][ticker]
                if hasattr(all_data.columns, "levels") and ticker in all_data.columns.levels[0]:
                    raw = all_data[ticker].copy()
                elif ticker in all_data.columns:
                    raw = all_data[ticker].copy()
            except Exception as slice_err:
                log.debug(f"{ticker}: failed to slice batch DataFrame: {slice_err}")
        result = _predict_one(ticker, raw)
        if result:
            predictions.append(result)
        else:
            log.warning(f"{ticker}: prediction skipped.")

    log.info(f"Predictions: {len(predictions)}/{len(tickers)} succeeded.")

    if not predictions:
        raise HTTPException(status_code=500, detail="All predictions failed.")

    # ΓöÇΓöÇ 3. Sentiment ΓÇö parallel network I/O, serial FinBERT ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
    # -- 3. Targeted Sentiment Inference ---------------------------------
    # Run FinBERT only on the top 35 candidates sorted by projected return
    # then confidence. Stocks with negative projections will never appear
    # in recommendations, so running transformer inference on them wastes
    # ~65% of total CPU time. This reduces FinBERT load from 100 tickers
    # to 35 without affecting recommendation quality.
    sorted_preds = sorted(
        [p for p in predictions if p.change_pct > 0],
        key=lambda x: (x.change_pct, x.confidence),
        reverse=True,
    )
    # Fallback: if fewer than 15 positive predictions, use top 35 overall
    if len(sorted_preds) < 15:
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
        future_to_ticker = {ex.submit(_gather_ticker, t): t for t in predicted_tickers}
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

    # ΓöÇΓöÇ 4. Risk rules ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
    log.info("Applying risk rules...")
    pred_dicts = [p.model_dump() for p in predictions]
    sent_dicts = [s.model_dump() for s in sentiments]

    try:
        enriched = apply_risk_rules(pred_dicts, sent_dicts)
    except ValueError as e:
        # apply_risk_rules raises ValueError when < MIN_RECORDS survive
        log.error(str(e))
        raise HTTPException(status_code=500, detail=str(e))

    log.info(f"Risk rules complete: {len(enriched)} records.")

    # ΓöÇΓöÇ 5. Build response (only the 15 PredictionRecordDto fields) ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
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
        )
        for r in enriched
    ]

    return ScoreResponse(
        generated_at = datetime.utcnow().isoformat(),
        count        = len(records),
        records      = records,
    )
