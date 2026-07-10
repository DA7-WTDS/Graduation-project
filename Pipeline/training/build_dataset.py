# QuantWise — ranking-dataset builder (IMPLEMENTATION_PLAN § 1.1).
#
# Builds the cross-sectional RELATIVE-return dataset that replaces absolute
# 30-day return regression:
#
#   label_rel(stock, t) = fwd_30d_return(stock, t) − median(fwd_30d_return(universe, t))
#   beat_median(stock, t) = label_rel > 0        (50% base rate by construction)
#
# The market component of returns (the unpredictable part) hits every name in
# the universe on the same date, so subtracting the per-date median cancels it.
# Ranking within one market is the only comparison the portfolio engine needs.
#
# Known limitation (documented in the plan): the universe is TODAY'S large-caps
# over a past window — survivorship bias. Fixed at the licensed-data migration
# (point-in-time constituents).
#
# Usage:
#   python -m training.build_dataset --market us --period 10y --out training/data/us_ranking.pkl

from __future__ import annotations

import argparse
import logging
import sys
from pathlib import Path

import numpy as np
import pandas as pd

sys.path.insert(0, str(Path(__file__).parent.parent))

from core.data_provider import get_provider  # noqa: E402
from core.features import compute_features   # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

HORIZON_DAYS = 21          # forward horizon in TRADING days (~30 calendar days)
MIN_NAMES_PER_DATE = 40    # cross-sections thinner than this are dropped

# The 14 technical indicators (matches models/universal_config.json tech_cols).
TECH_COLS = [
    "Volume_Ratio", "RSI", "MACD", "MACD_signal", "MACD_hist",
    "SMA_5_Ratio", "SMA_10_Ratio", "SMA_15_Ratio", "SMA_30_Ratio",
    "EMA_9_Ratio", "Volatility_20", "Momentum_10", "Momentum_21", "Volume_Change",
]

# The 5 LSTM sequential inputs (universal_config feature_cols). Only "Return"
# is not already in TECH_COLS; carried so experiments can window embeddings.
SEQ_COLS = ["Volume_Ratio", "Return", "RSI", "MACD", "MACD_signal"]


def build(market: str, period: str, out_path: Path) -> pd.DataFrame:
    provider = get_provider(market)
    tickers = provider.get_universe()
    log.info(f"Universe: {len(tickers)} tickers · downloading {period} of daily OHLCV...")

    data = provider.get_ohlcv_batch(tickers, period=period)
    if data is None or data.empty:
        raise SystemExit("Download failed / empty.")

    frames: list[pd.DataFrame] = []
    is_multi = hasattr(data.columns, "levels")

    for t in tickers:
        try:
            raw = data[t].dropna(how="all") if is_multi else data.dropna(how="all")
        except Exception:
            continue
        if raw is None or raw.empty:
            continue

        df = raw.reset_index()
        df.columns = [str(c).lower() for c in df.columns]
        date_col = next((c for c in df.columns if "date" in c), None)
        if date_col is None:
            continue
        df = df.rename(columns={date_col: "date"})
        df = df[["date", "open", "high", "low", "close", "volume"]].dropna(subset=["close"])
        df = df.sort_values("date").reset_index(drop=True)
        if len(df) < 250:  # need at least ~1y usable history
            continue

        df = compute_features(df)
        if df.empty:
            continue

        # Forward return over the horizon (trading days), then the raw label.
        df["fwd_return"] = df["close"].shift(-HORIZON_DAYS) / df["close"] - 1.0
        df["ticker"] = t
        cols = ["date", "ticker", "close", "fwd_return", *dict.fromkeys(TECH_COLS + SEQ_COLS)]
        frames.append(df[cols])

    if not frames:
        raise SystemExit("No usable ticker frames.")

    panel = pd.concat(frames, ignore_index=True)
    panel = panel.dropna(subset=["fwd_return"])
    # Zero-volume days etc. produce inf in ratio features; XGBoost rejects inf.
    panel[TECH_COLS] = panel[TECH_COLS].replace([np.inf, -np.inf], np.nan)
    panel = panel.dropna(subset=TECH_COLS)
    panel["date"] = pd.to_datetime(panel["date"])

    # Cross-sectional relabel: subtract the per-date universe median.
    counts = panel.groupby("date")["ticker"].transform("count")
    panel = panel[counts >= MIN_NAMES_PER_DATE].copy()
    median_by_date = panel.groupby("date")["fwd_return"].transform("median")
    panel["rel_return"] = panel["fwd_return"] - median_by_date
    panel["beat_median"] = (panel["rel_return"] > 0).astype(int)

    panel = panel.sort_values(["date", "ticker"]).reset_index(drop=True)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    panel.to_pickle(out_path)

    log.info(
        f"Dataset: {len(panel):,} rows · {panel['ticker'].nunique()} tickers · "
        f"{panel['date'].min().date()} → {panel['date'].max().date()} · "
        f"beat_median base rate {panel['beat_median'].mean():.3f} (≈0.5 by construction)"
    )
    log.info(f"Wrote {out_path}")
    return panel


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--market", default="us")
    ap.add_argument("--period", default="10y")
    ap.add_argument("--out", default="training/data/us_ranking.pkl")
    args = ap.parse_args()
    build(args.market, args.period, Path(args.out))
