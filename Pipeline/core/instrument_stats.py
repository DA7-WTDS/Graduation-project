# QuantWise — instrument statistics, computed as of a point in time.
#
# These three numbers are not decoration: the allocation optimizer weights every
# core position by score / realized_vol, caps sectors, and gates the tactical
# sleeve on average daily value traded. So a stat measured at the wrong moment
# does not merely mislabel a row — it changes the portfolio.
#
# That is why `as_of` exists. Replaying a 2025 portfolio while weighting it by
# 2026 volatility is lookahead: measured across the replayed universe, ~23% of
# names shifted vol by more than 30% over a single year, and the biggest movers
# shifted by 1.5-2.2x. Whether that flatters or penalises depends on the regime
# (in a melt-up, rising vol accompanies the winners and under-weighting them
# understates the result; in a drawdown the same mechanism flatters), which is
# precisely why it cannot be waved through as "small".
#
# Truncating the price frame at `as_of` is the whole guarantee: vol, ADV and the
# last close all derive from the same slice, so none of them can see past it.

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd

# Trading days in a year — the vol window, and the annualization factor.
TRADING_DAYS = 252

# Below this many observations a volatility estimate is noise, so it is reported
# as unknown rather than as a number the optimizer would weight positions by.
MIN_OBSERVATIONS = 60

# Live behaviour: average daily value traded over the trailing quarter.
ADV_WINDOW = 90


@dataclass(frozen=True)
class Stats:
    realized_vol_1y: float | None
    avg_daily_value_traded: float | None
    last_close: float | None


EMPTY = Stats(None, None, None)


def truncate(frame: pd.DataFrame, as_of: pd.Timestamp | None) -> pd.DataFrame:
    """The price history knowable at `as_of`, trimmed to the vol window.

    Timezone-aware indices are flattened first: yfinance returns tz-aware frames
    for some tickers and naive for others, and comparing the two raises rather
    than silently misfiltering — but only for the subset that happens to be aware,
    which would look like a per-ticker data problem instead of a bug here.
    """
    if frame is None or len(frame) == 0 or as_of is None:
        return frame
    idx = pd.to_datetime(frame.index)
    if getattr(idx, "tz", None) is not None:
        idx = idx.tz_localize(None)
    return frame[idx <= as_of].tail(TRADING_DAYS)


def compute(frame: pd.DataFrame, as_of: pd.Timestamp | None = None) -> Stats:
    """Volatility, traded value and last close from one OHLCV frame.

    With `as_of` set, every figure is as it stood at that close. Without it, the
    frame is used as given — the live nightly path.
    """
    frame = truncate(frame, as_of)
    if frame is None or len(frame) < MIN_OBSERVATIONS or "Close" not in frame:
        return EMPTY

    closes = frame["Close"].dropna()
    if closes.empty:
        return EMPTY

    returns = closes.pct_change().dropna()
    vol = float(returns.std() * np.sqrt(TRADING_DAYS)) if len(returns) >= MIN_OBSERVATIONS else None

    adv = None
    if "Volume" in frame:
        traded = (frame["Close"] * frame["Volume"]).dropna().tail(ADV_WINDOW)
        if not traded.empty:
            adv = float(traded.mean())

    return Stats(
        realized_vol_1y=vol,
        avg_daily_value_traded=adv,
        last_close=float(closes.iloc[-1]),
    )
