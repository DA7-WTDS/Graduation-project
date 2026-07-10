# QuantWise — walk-forward portfolio backtester (IMPLEMENTATION_PLAN § 1.8).
#
# Turns model ranks into the number that actually matters: what a portfolio
# following them would have earned, AFTER transaction costs, vs honest
# benchmarks. This is the promotion gate's evidence (§ 1.7) and the source of
# any public track-record claim.
#
# Simulation (deliberately simple and auditable):
#   • OOS window only — the same held-out test split the trainer never touched.
#   • Every 21 trading days: rank by model score → take top N (default 10)
#     → weight ∝ 1/Volatility_20, capped per position, renormalized.
#   • Between rebalances weights drift with prices (buy-and-hold).
#   • Costs: 25 bps one-side on traded weight at each rebalance.
#   • Benchmarks: S&P 500 (^GSPC) and the equal-weight universe rebalanced on
#     the same dates with the same cost model.
#
# US-only for now; EGX price-limit fill modeling arrives with EGX data (§ 0.1).
#
# Usage:
#   python -m training.backtest --data training/data/us_ranking.pkl --model models/ranking_v1

from __future__ import annotations

import argparse
import json
import logging
import sys
from pathlib import Path

import numpy as np
import pandas as pd
import xgboost as xgb

sys.path.insert(0, str(Path(__file__).parent.parent))

from core.data_provider import get_provider                     # noqa: E402
from training.train_ranking import chrono_split                 # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

REBALANCE_EVERY = 21     # trading days
TOP_N = 10
MAX_WEIGHT = 0.15
COST_ONE_SIDE = 0.0025   # 25 bps per side
TRADING_DAYS = 252


def stats(equity: pd.Series, label: str) -> dict:
    ret = equity.pct_change().dropna()
    years = len(ret) / TRADING_DAYS
    cagr = float(equity.iloc[-1] ** (1 / years) - 1) if years > 0 else 0.0
    vol = float(ret.std() * np.sqrt(TRADING_DAYS))
    sharpe = float(ret.mean() / ret.std() * np.sqrt(TRADING_DAYS)) if ret.std() > 0 else 0.0
    dd = float((equity / equity.cummax() - 1).min())
    out = {
        "total_return_pct": round(float(equity.iloc[-1] - 1) * 100, 2),
        "cagr_pct": round(cagr * 100, 2),
        "ann_vol_pct": round(vol * 100, 2),
        "sharpe": round(sharpe, 2),
        "max_drawdown_pct": round(dd * 100, 2),
    }
    log.info(f"{label:14s}: total {out['total_return_pct']:+.1f}% · CAGR {out['cagr_pct']:+.1f}% · "
             f"Sharpe {out['sharpe']:.2f} · maxDD {out['max_drawdown_pct']:.1f}%")
    return out


def simulate(dates: list, daily_ret: pd.DataFrame, pick_weights) -> tuple[pd.Series, dict]:
    """Generic engine: pick_weights(rebalance_date) -> {ticker: weight}.
    Returns (equity curve, {turnover, cost_drag})."""
    rebal_dates = dates[::REBALANCE_EVERY]
    equity, eq = [], 1.0
    weights: pd.Series = pd.Series(dtype=float)
    turnover_total = cost_total = 0.0
    out_index = []

    for d in dates:
        if d in rebal_dates:
            target = pd.Series(pick_weights(d), dtype=float)
            if not target.empty:
                union = weights.index.union(target.index)
                drifted = weights.reindex(union, fill_value=0.0)
                tgt = target.reindex(union, fill_value=0.0)
                traded = float((tgt - drifted).abs().sum()) / 2  # one-sided
                cost = traded * COST_ONE_SIDE * 2                # both sides of the swap
                eq *= (1 - cost)
                turnover_total += traded
                cost_total += cost
                weights = tgt[tgt > 0]
        if not weights.empty:
            r = daily_ret.loc[d].reindex(weights.index).fillna(0.0)
            day_ret = float((weights * r).sum())
            eq *= (1 + day_ret)
            grown = weights * (1 + r)
            weights = grown / grown.sum()
        equity.append(eq)
        out_index.append(d)

    n_rebals = max(1, len(rebal_dates))
    return pd.Series(equity, index=out_index), {
        "avg_turnover_per_rebalance": round(turnover_total / n_rebals, 3),
        "total_cost_drag_pct": round(cost_total * 100, 2),
    }


def main(data_path: Path, model_dir: Path) -> dict:
    panel: pd.DataFrame = pd.read_pickle(data_path)
    _, b2 = chrono_split(panel["date"])
    oos = panel[panel["date"] >= b2].copy()
    log.info(f"OOS window: {oos['date'].min().date()} → {oos['date'].max().date()}")

    features = json.loads((model_dir / "features.json").read_text())
    model = xgb.XGBRegressor()
    model.load_model(model_dir / "xgb_ranking.json")
    oos["score"] = model.predict(oos[features])

    closes = oos.pivot_table(index="date", columns="ticker", values="close")
    daily_ret = closes.pct_change().iloc[1:]
    dates = list(daily_ret.index)

    by_date = {d: g for d, g in oos.groupby("date")}

    def model_picks(d):
        g = by_date.get(d)
        if g is None:
            return {}
        top = g.nlargest(TOP_N, "score")
        inv_vol = 1.0 / top["Volatility_20"].clip(lower=1e-4)
        w = (inv_vol / inv_vol.sum()).clip(upper=MAX_WEIGHT)
        w = w / w.sum()
        return dict(zip(top["ticker"], w))

    def ew_picks(d):
        g = by_date.get(d)
        if g is None:
            return {}
        return {t: 1.0 / len(g) for t in g["ticker"]}

    log.info(f"Simulating: top-{TOP_N}, inverse-vol, {MAX_WEIGHT:.0%} cap, "
             f"rebalance {REBALANCE_EVERY}d, {COST_ONE_SIDE*1e4:.0f} bps/side...")
    strat_eq, strat_trade = simulate(dates, daily_ret, model_picks)
    ew_eq, _ = simulate(dates, daily_ret, ew_picks)

    # S&P 500 over the same window.
    provider = get_provider("us")
    spx_raw = provider.get_ohlcv_batch(["^GSPC"], period="3y")
    spx = spx_raw["^GSPC"]["Close"].dropna()
    spx.index = pd.to_datetime(spx.index).tz_localize(None)
    spx = spx.reindex(pd.DatetimeIndex(dates)).ffill()
    spx_eq = spx / spx.iloc[0]

    result = {
        "window": f"{dates[0].date()} → {dates[-1].date()}",
        "config": {"top_n": TOP_N, "rebalance_days": REBALANCE_EVERY,
                   "max_weight": MAX_WEIGHT, "cost_bps_per_side": COST_ONE_SIDE * 1e4},
        "strategy": {**stats(strat_eq, "strategy"), **strat_trade},
        "equal_weight_universe": stats(ew_eq, "equal-weight"),
        "sp500": stats(spx_eq, "S&P 500"),
        "note": "OOS test split only; survivorship-biased universe (today's constituents) inflates ALL three series equally — the strategy-vs-benchmark GAP is the honest signal.",
    }

    curves = pd.DataFrame({"strategy": strat_eq, "equal_weight": ew_eq, "sp500": spx_eq})
    curves.index.name = "date"
    curves.to_csv(model_dir / "backtest_curves.csv")
    (model_dir / "backtest.json").write_text(json.dumps(result, indent=2))
    log.info(f"Artifacts → {model_dir}/backtest.json, backtest_curves.csv")
    return result


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default="training/data/us_ranking.pkl")
    ap.add_argument("--model", default="models/ranking_v1")
    args = ap.parse_args()
    main(Path(args.data), Path(args.model))
