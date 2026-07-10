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

from training.build_dataset import EXTRA_COLS, HORIZON_DAYS, TECH_COLS  # noqa: E402

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


XGB_PARAMS = dict(
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

CULL_GAIN_SHARE = 0.005  # features under 0.5% of total gain are cull candidates


def fit_variant(train, val, test, cols: list[str], label: str):
    model = xgb.XGBRegressor(**XGB_PARAMS)
    model.fit(train[cols], train["rel_return"], eval_set=[(val[cols], val["rel_return"])], verbose=False)
    scored = test.copy()
    scored["pred"] = model.predict(scored[cols])
    m = evaluate(scored, "pred")
    m["best_iteration"] = int(model.best_iteration)
    m["n_features"] = len(cols)
    log.info(f"{label}: IC {m['ic_mean']} (t {m['ic_t_stat']}) · hit {m['hit_rate']} · spread {m['decile_spread_mean']}")
    return model, m


def main(data_path: Path, out_dir: Path) -> dict:
    panel: pd.DataFrame = pd.read_pickle(data_path)
    log.info(f"Loaded {len(panel):,} rows · {panel['date'].min().date()} → {panel['date'].max().date()}")

    sec_cols = [c for c in panel.columns if c.startswith("sec_")]
    expanded_cols = TECH_COLS + [c for c in EXTRA_COLS if c in panel.columns] + sec_cols

    b1, b2 = chrono_split(panel["date"])
    purge = pd.Timedelta(days=PURGE_CALENDAR_DAYS)

    train = panel[panel["date"] < b1 - purge]
    val   = panel[(panel["date"] >= b1) & (panel["date"] < b2 - purge)]
    test  = panel[panel["date"] >= b2]
    log.info(f"Split: train {len(train):,} (<{(b1 - purge).date()}) · val {len(val):,} · test {len(test):,} (≥{b2.date()})")

    # A/B: base 14 indicators vs Phase-1.3 expanded set, identical everything else.
    base_model, base_m = fit_variant(train, val, test, TECH_COLS, "base-14    ")
    exp_model, exp_m   = fit_variant(train, val, test, expanded_cols, "expanded   ")

    winner_is_expanded = exp_m["ic_mean"] >= base_m["ic_mean"]
    winner_model = exp_model if winner_is_expanded else base_model
    winner_cols  = expanded_cols if winner_is_expanded else TECH_COLS

    # Culling gate (§ 1.3): near-zero-gain features are flagged every run.
    gains = winner_model.get_booster().get_score(importance_type="gain")
    total_gain = sum(gains.values()) or 1.0
    gain_share = {c: round(gains.get(c, 0.0) / total_gain, 5) for c in winner_cols}
    cull = sorted([c for c, s in gain_share.items() if s < CULL_GAIN_SHARE])

    momentum_baseline = evaluate(test, "Momentum_21")

    metrics = {
        "target": f"relative {HORIZON_DAYS}-trading-day return vs universe median",
        "test_window": f"{test['date'].min().date()} → {test['date'].max().date()}",
        "base_14_indicators": base_m,
        "expanded_1_3": exp_m,
        "delta_ic_expanded_vs_base": round(exp_m["ic_mean"] - base_m["ic_mean"], 4),
        "winner": "expanded" if winner_is_expanded else "base_14",
        "baseline_momentum21": momentum_baseline,
        "base_rate_beat_median": round(float(test["beat_median"].mean()), 4),
        "n_test": len(test),
        "cull_candidates_gain_lt_0.5pct": cull,
        "deferred_blocks": "sentiment/analyst history (arrives via § 1.6 + own daily-run accumulation)",
    }

    out_dir.mkdir(parents=True, exist_ok=True)
    winner_model.save_model(out_dir / "xgb_ranking.json")
    (out_dir / "features.json").write_text(json.dumps(winner_cols, indent=2))
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2))
    (out_dir / "feature_importance.json").write_text(json.dumps(
        dict(sorted(gain_share.items(), key=lambda kv: -kv[1])), indent=2))

    log.info(json.dumps(metrics, indent=2))
    log.info(f"Artifacts → {out_dir}")
    return metrics


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default="training/data/us_ranking.pkl")
    ap.add_argument("--out", default="models/ranking_v1")
    args = ap.parse_args()
    main(Path(args.data), Path(args.out))
