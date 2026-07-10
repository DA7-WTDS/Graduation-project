# QuantWise — ranking-model trainer + honest evaluation (IMPLEMENTATION_PLAN § 1.1).
#
# Trains an XGBoost regressor on the cross-sectional RELATIVE 30-day target
# produced by build_dataset.py and evaluates it the way a portfolio engine
# actually consumes it: as a within-date ranking.
#
# Metrics reported on the held-out test years:
#   • IC        — mean daily Spearman correlation (predicted vs realized relative
#                 return). The standard cross-sectional skill measure; >0.03 is real.
#   • hit-rate  — P(pred says beat-median & it did) vs the 50% base rate that the
#                 label guarantees BY CONSTRUCTION (no always-up artifact possible).
#   • decile spread — mean(top-decile realized rel return) − mean(bottom decile),
#                 per date: what long-the-best/avoid-the-worst is actually worth.
#   • Momentum_21 baseline — the same metrics using the naive momentum feature as
#                 the score, so the model must beat the simplest possible signal.
#
# Chronological 70/15/15 split by DATE with a purge gap of the label horizon
# between segments (a forward window that crosses a boundary would leak).
#
# Artifacts (NOT wired into serving — champion/challenger gate comes in § 1.7):
#   models/ranking_v1/xgb_ranking.json, metrics.json, feature_importance.json
#
# Usage:
#   python -m training.train_ranking --data training/data/us_ranking.pkl --out models/ranking_v1

from __future__ import annotations

import argparse
import json
import logging
import sys
from pathlib import Path

import numpy as np
import pandas as pd
import xgboost as xgb
from scipy.stats import spearmanr

sys.path.insert(0, str(Path(__file__).parent.parent))

from training.build_dataset import HORIZON_DAYS, TECH_COLS  # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

PURGE_CALENDAR_DAYS = 45  # > horizon in calendar days; windows can't cross splits


def chrono_split(dates: pd.Series) -> tuple[pd.Timestamp, pd.Timestamp]:
    """70/15/15 boundaries over unique dates."""
    uniq = np.sort(dates.unique())
    return pd.Timestamp(uniq[int(len(uniq) * 0.70)]), pd.Timestamp(uniq[int(len(uniq) * 0.85)])


def evaluate(df: pd.DataFrame, score_col: str) -> dict:
    """Rank-quality metrics of `score_col` against realized rel_return, per date."""
    ics, spreads = [], []
    for _, day in df.groupby("date"):
        if len(day) < 20:
            continue
        ic = spearmanr(day[score_col], day["rel_return"]).statistic
        if not np.isnan(ic):
            ics.append(ic)
        deciles = pd.qcut(day[score_col].rank(method="first"), 10, labels=False)
        spreads.append(
            day.loc[deciles == 9, "rel_return"].mean() - day.loc[deciles == 0, "rel_return"].mean()
        )
    hits = ((df[score_col] > 0) == (df["rel_return"] > 0)).mean()
    return {
        "ic_mean": round(float(np.mean(ics)), 4),
        "ic_std": round(float(np.std(ics)), 4),
        "ic_t_stat": round(float(np.mean(ics) / (np.std(ics) / np.sqrt(len(ics)))), 2),
        "hit_rate": round(float(hits), 4),
        "decile_spread_mean": round(float(np.mean(spreads)), 4),
        "days": len(ics),
    }


def main(data_path: Path, out_dir: Path) -> dict:
    panel: pd.DataFrame = pd.read_pickle(data_path)
    log.info(f"Loaded {len(panel):,} rows · {panel['date'].min().date()} → {panel['date'].max().date()}")

    b1, b2 = chrono_split(panel["date"])
    purge = pd.Timedelta(days=PURGE_CALENDAR_DAYS)

    train = panel[panel["date"] < b1 - purge]
    val   = panel[(panel["date"] >= b1) & (panel["date"] < b2 - purge)]
    test  = panel[panel["date"] >= b2]
    log.info(f"Split: train {len(train):,} (<{(b1 - purge).date()}) · val {len(val):,} · test {len(test):,} (≥{b2.date()})")

    model = xgb.XGBRegressor(
        n_estimators=600,
        learning_rate=0.03,
        max_depth=5,
        subsample=0.8,
        colsample_bytree=0.8,
        min_child_weight=20,
        objective="reg:squarederror",
        early_stopping_rounds=50,
        random_state=42,
        n_jobs=-1,
    )
    model.fit(
        train[TECH_COLS], train["rel_return"],
        eval_set=[(val[TECH_COLS], val["rel_return"])],
        verbose=False,
    )
    log.info(f"Trained: best_iteration={model.best_iteration}")

    test = test.copy()
    test["pred"] = model.predict(test[TECH_COLS])

    metrics = {
        "target": f"relative {HORIZON_DAYS}-trading-day return vs universe median",
        "features": "14 technical indicators (same as deployed tech_cols)",
        "test_window": f"{test['date'].min().date()} → {test['date'].max().date()}",
        "model": evaluate(test, "pred"),
        "baseline_momentum21": evaluate(test, "Momentum_21"),
        "base_rate_beat_median": round(float(test["beat_median"].mean()), 4),
        "n_test": len(test),
        "best_iteration": int(model.best_iteration),
    }

    out_dir.mkdir(parents=True, exist_ok=True)
    model.save_model(out_dir / "xgb_ranking.json")
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2))
    importance = dict(sorted(
        zip(TECH_COLS, model.feature_importances_.round(4).tolist()),
        key=lambda kv: -kv[1]))
    (out_dir / "feature_importance.json").write_text(json.dumps(importance, indent=2))

    log.info(json.dumps(metrics, indent=2))
    log.info(f"Artifacts → {out_dir}")
    return metrics


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default="training/data/us_ranking.pkl")
    ap.add_argument("--out", default="models/ranking_v1")
    args = ap.parse_args()
    main(Path(args.data), Path(args.out))
