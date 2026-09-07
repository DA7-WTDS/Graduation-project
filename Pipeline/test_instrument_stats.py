# Tests for core/instrument_stats.py — the point-in-time guarantee behind the
# optimizer's weighting inputs (MVP_PLAN § C).
# Standalone, no pytest, no network:  python test_instrument_stats.py

import numpy as np
import pandas as pd

from core import instrument_stats as istats


def frame(days=400, start="2025-01-01", vol=0.01, price=100.0, volume=1_000_000, seed=3, tz=None):
    """A synthetic OHLCV frame with a known volatility regime."""
    rng = np.random.default_rng(seed)
    idx = pd.bdate_range(start=start, periods=days)
    if tz:
        idx = idx.tz_localize(tz)
    rets = rng.normal(0, vol, days)
    closes = price * np.exp(np.cumsum(rets))
    return pd.DataFrame({"Close": closes, "Volume": [volume] * days}, index=idx)


def two_regimes():
    """Calm for a year, then wild for a year — so a point-in-time read taken
    during the calm stretch must not see the wild one."""
    calm = frame(days=300, start="2025-01-01", vol=0.005, seed=1)
    wild = frame(days=300, start="2026-03-01", vol=0.04, price=float(calm["Close"].iloc[-1]), seed=2)
    return pd.concat([calm, wild])


# ---- the guarantee ---------------------------------------------------------

def test_as_of_cannot_see_later_volatility():
    """The whole point: weighting a 2025 portfolio by 2026 vol is lookahead."""
    f = two_regimes()
    early = istats.compute(f, pd.Timestamp("2025-12-01"))
    late = istats.compute(f, pd.Timestamp("2027-01-01"))
    assert early.realized_vol_1y < late.realized_vol_1y / 2, (early, late)


def test_as_of_price_is_the_close_on_that_date():
    f = frame(days=300, start="2025-01-01")
    asof = pd.Timestamp("2025-06-02")
    got = istats.compute(f, asof).last_close
    expected = float(f[f.index <= asof]["Close"].iloc[-1])
    assert got == expected


def test_adv_is_also_point_in_time():
    f = pd.concat([
        frame(days=200, start="2025-01-01", volume=1_000_000, seed=4),
        frame(days=200, start="2025-10-10", volume=50_000_000, seed=5),
    ])
    early = istats.compute(f, pd.Timestamp("2025-09-01"))
    late = istats.compute(f, pd.Timestamp("2026-06-01"))
    assert early.avg_daily_value_traded < late.avg_daily_value_traded / 5


def test_without_as_of_the_frame_is_used_as_given():
    """The live nightly path must be untouched by any of this."""
    f = frame(days=300)
    assert istats.compute(f).last_close == float(f["Close"].iloc[-1])


# ---- windowing -------------------------------------------------------------

def test_vol_window_is_capped_at_a_year():
    """Five years of history must still produce a 1-year vol, or the number stops
    meaning what its name says."""
    long_history = frame(days=1300, start="2021-01-01")
    truncated = istats.truncate(long_history, pd.Timestamp("2026-01-01"))
    assert len(truncated) == istats.TRADING_DAYS


def test_timezone_aware_frames_do_not_raise():
    """yfinance returns tz-aware frames for some tickers and naive for others.
    Comparing the two raises, and it would look like a per-ticker data fault."""
    aware = frame(days=300, tz="America/New_York")
    got = istats.compute(aware, pd.Timestamp("2025-06-02"))
    assert got.last_close is not None


# ---- refusing to guess -----------------------------------------------------

def test_thin_history_reports_unknown_rather_than_a_noisy_number():
    """The optimizer weights positions by 1/vol. A vol estimated from 10 points
    would silently produce a real position size."""
    got = istats.compute(frame(days=30), pd.Timestamp("2030-01-01"))
    assert got == istats.EMPTY


def test_as_of_before_any_history_is_empty_not_an_error():
    got = istats.compute(frame(days=300, start="2025-01-01"), pd.Timestamp("2020-01-01"))
    assert got == istats.EMPTY


def test_missing_volume_still_gives_vol_and_price():
    f = frame(days=300)[["Close"]]
    got = istats.compute(f, pd.Timestamp("2025-06-02"))
    assert got.realized_vol_1y is not None and got.last_close is not None
    assert got.avg_daily_value_traded is None


def test_empty_frame_is_empty():
    assert istats.compute(pd.DataFrame(), pd.Timestamp("2026-01-01")) == istats.EMPTY
    assert istats.compute(None, pd.Timestamp("2026-01-01")) == istats.EMPTY


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
