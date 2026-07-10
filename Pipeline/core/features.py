# QuantWise — shared feature engineering.
#
# Single implementation used by BOTH serving (main.py) and training
# (training/build_dataset.py). Must stay in lockstep with the artifacts in
# models/ (scalers were fit on exactly these columns). Any change here is a
# model-retrain event, not a refactor.

from __future__ import annotations

import numpy as np
import pandas as pd


def compute_rsi(series: pd.Series, period: int = 14) -> pd.Series:
    delta    = series.diff()
    gain     = delta.where(delta > 0, 0.0)
    loss     = (-delta).where(delta < 0, 0.0)
    avg_gain = gain.rolling(period).mean()
    avg_loss = loss.rolling(period).mean()
    # Guard against all-up windows (avg_loss == 0 → rs = inf → NaN)
    avg_loss = avg_loss.replace(0, np.nan)
    rs       = avg_gain / avg_loss
    return (100.0 - (100.0 / (1.0 + rs))).fillna(100.0)


def compute_features(df: pd.DataFrame) -> pd.DataFrame:
    """Adds the 14 technical-indicator columns (+ Return) to a lowercase OHLCV
    frame sorted by date. Drops warmup rows. Identical to the training notebook."""
    df = df.copy()
    close  = df["close"]
    volume = df["volume"]

    df["Return"]       = close.pct_change()
    vol_sma20          = volume.rolling(20).mean()
    df["Volume_Ratio"] = volume / vol_sma20
    df["RSI"]          = compute_rsi(close, period=14)

    ema_12             = close.ewm(span=12, min_periods=12).mean()
    ema_26             = close.ewm(span=26, min_periods=26).mean()
    df["MACD"]         = (ema_12 - ema_26) / close
    df["MACD_signal"]  = df["MACD"].ewm(span=9, min_periods=9).mean()
    df["MACD_hist"]    = df["MACD"] - df["MACD_signal"]

    for window in [5, 10, 15, 30]:
        # shift(1) prevents look-ahead bias — yesterday's SMA vs today's close
        df[f"SMA_{window}_Ratio"] = (close.rolling(window).mean().shift(1) / close).fillna(1.0)
    df["EMA_9_Ratio"]   = (close.ewm(span=9).mean().shift(1) / close).fillna(1.0)
    df["Volatility_20"] = close.pct_change().rolling(20).std()
    df["Momentum_10"]   = close.pct_change(periods=10)
    df["Momentum_21"]   = close.pct_change(periods=21)
    df["Volume_Change"] = volume.pct_change()
    df["Volume_Ratio"]  = df["Volume_Ratio"].replace([np.inf, -np.inf], np.nan)

    df.dropna(inplace=True)
    df.reset_index(drop=True, inplace=True)
    return df
