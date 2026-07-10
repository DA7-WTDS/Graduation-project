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
import json
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

# Phase 1.3 expansion (IMPLEMENTATION_PLAN § 1.3) — three blocks buildable from
# 10y of real history today. Sentiment/analyst blocks are DEFERRED: no free
# historical source exists; they accumulate from our own daily runs and enter
# training via § 1.6. Market-cap bucket deliberately excluded (today's cap on
# historic rows = look-ahead).
#
# Note: the mkt_*/vix_* features are constant within each date's cross-section —
# they cannot move the ranking directly; their value is REGIME CONDITIONING
# (trees learn e.g. "momentum ranks differently when VIX is high").
MACRO_COLS = ["mkt_ret_21", "mkt_ret_63", "mkt_vol_20", "vix", "vix_chg_21"]
REL_COLS   = ["ret_63", "rel_mom_21", "rel_mom_63", "vol_ratio"]
EXTRA_COLS = MACRO_COLS + REL_COLS  # + dynamic sec_* one-hots

INDEX_SYMBOL = "^GSPC"
VIX_SYMBOL   = "^VIX"


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
        df["ret_63"] = df["close"].pct_change(63)
        df["ticker"] = t
        cols = ["date", "ticker", "close", "fwd_return", "ret_63",
                *dict.fromkeys(TECH_COLS + SEQ_COLS)]
        frames.append(df[cols])

    if not frames:
        raise SystemExit("No usable ticker frames.")

    panel = pd.concat(frames, ignore_index=True)
    panel = panel.dropna(subset=["fwd_return"])
    panel["date"] = pd.to_datetime(panel["date"])

    # ---- macro/regime block: S&P 500 + VIX context per date ----
    log.info("Downloading market context (^GSPC, ^VIX)...")
    ctx_raw = provider.get_ohlcv_batch([INDEX_SYMBOL, VIX_SYMBOL], period=period)
    spx = ctx_raw[INDEX_SYMBOL]["Close"].dropna()
    vix = ctx_raw[VIX_SYMBOL]["Close"].dropna()
    ctx = pd.DataFrame({
        "mkt_ret_21": spx.pct_change(21),
        "mkt_ret_63": spx.pct_change(63),
        "mkt_vol_20": spx.pct_change().rolling(20).std(),
        "vix": vix,
        "vix_chg_21": vix.pct_change(21),
    })
    ctx.index = pd.to_datetime(ctx.index).tz_localize(None)
    ctx = ctx.reset_index().rename(columns={ctx.index.name or "index": "date"})
    ctx.columns = ["date", *MACRO_COLS]
    panel = panel.merge(ctx, on="date", how="inner")

    # ---- relative-strength block: stock vs market ----
    panel["rel_mom_21"] = panel["Momentum_21"] - panel["mkt_ret_21"]
    panel["rel_mom_63"] = panel["ret_63"] - panel["mkt_ret_63"]
    panel["vol_ratio"]  = panel["Volatility_20"] / panel["mkt_vol_20"]

    # ---- sector one-hot (cached; ~stable attribute, one vendor call/ticker) ----
    sector_cache = Path("training/data") / f"sectors_{market}.json"
    if sector_cache.exists():
        sector_map = json.loads(sector_cache.read_text())
    else:
        log.info("Fetching sector map (one-time, cached)...")
        sector_map = provider.get_sector_map(sorted(panel["ticker"].unique()))
        sector_cache.parent.mkdir(parents=True, exist_ok=True)
        sector_cache.write_text(json.dumps(sector_map, indent=2))
    panel["sector"] = panel["ticker"].map(sector_map).fillna("Unknown")
    sec_dummies = pd.get_dummies(panel["sector"].str.replace(" ", "_"), prefix="sec", dtype=int)
    panel = pd.concat([panel, sec_dummies], axis=1)

    # Zero-volume days etc. produce inf in ratio features; XGBoost rejects inf.
    all_numeric = TECH_COLS + EXTRA_COLS
    panel[all_numeric] = panel[all_numeric].replace([np.inf, -np.inf], np.nan)
    panel = panel.dropna(subset=all_numeric)

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
