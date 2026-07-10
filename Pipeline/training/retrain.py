# QuantWise — walk-forward retrain + champion/challenger promotion (§ 1.7).
#
# The static-model problem killer: run monthly (cron / scheduled task). Each run:
#   1. Rebuilds the dataset on the expanding window (fresh data).
#   2. Trains a challenger (train_ranking's full A/B + calibration).
#   3. Evaluates CHAMPION and CHALLENGER on the *same fresh OOS test slice* —
#      rows neither model trained on.
#   4. Promotes the challenger into models/ranking_v1 ONLY if it wins
#      (IC ≥ champion + margin, hit-rate not worse than −0.5pp).
#   5. Appends an immutable entry to models/registry.json either way.
#
# No champion yet → bootstrap-promotes the first candidate.
#
# Usage (monthly):
#   python -m training.retrain --market us --period 10y

from __future__ import annotations

import argparse
import hashlib
import json
import logging
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

import pandas as pd
import xgboost as xgb

sys.path.insert(0, str(Path(__file__).parent.parent))

from training import build_dataset as bd                        # noqa: E402
from training.train_ranking import (                            # noqa: E402
    PURGE_CALENDAR_DAYS, chrono_split, evaluate, main as train_main)

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

PIPELINE_DIR = Path(__file__).parent.parent
CHAMPION_DIR = PIPELINE_DIR / "models" / "ranking_v1"
REGISTRY = PIPELINE_DIR / "models" / "registry.json"

PROMOTE_IC_MARGIN = 0.0      # challenger must at least match champion IC
PROMOTE_HIT_TOLERANCE = 0.005  # and not lose more than 0.5pp hit-rate


def eval_on_slice(model_dir: Path, test: pd.DataFrame) -> dict | None:
    """Evaluate an existing model's artifacts on a fresh OOS slice."""
    try:
        features = json.loads((model_dir / "features.json").read_text())
        model = xgb.XGBRegressor()
        model.load_model(model_dir / "xgb_ranking.json")
    except Exception as e:
        log.warning(f"No usable champion at {model_dir} ({e}).")
        return None
    scored = test.copy()
    scored["pred"] = model.predict(scored[features])
    return evaluate(scored, "pred")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()[:16]


def main(market: str, period: str) -> dict:
    ts = datetime.now(timezone.utc).strftime("%Y%m%d_%H%M%S")
    data_path = PIPELINE_DIR / "training" / "data" / f"{market}_ranking.pkl"
    candidate_dir = PIPELINE_DIR / "models" / f"candidate_{ts}"

    # 1-2. Fresh dataset + challenger training (full A/B + calibration inside).
    bd.build(market, period, data_path)
    challenger_metrics = train_main(data_path, candidate_dir)

    # 3. Same-slice comparison.
    panel: pd.DataFrame = pd.read_pickle(data_path)
    _, b2 = chrono_split(panel["date"])
    test = panel[panel["date"] >= b2]

    challenger_eval = eval_on_slice(candidate_dir, test)
    champion_eval = eval_on_slice(CHAMPION_DIR, test)

    if champion_eval is None:
        promoted, reason = True, "bootstrap (no champion)"
    else:
        d_ic = challenger_eval["ic_mean"] - champion_eval["ic_mean"]
        d_hit = challenger_eval["hit_rate"] - champion_eval["hit_rate"]
        promoted = (d_ic >= PROMOTE_IC_MARGIN) and (d_hit >= -PROMOTE_HIT_TOLERANCE)
        reason = f"dIC {d_ic:+.4f}, dHit {d_hit:+.4f} vs rule (IC ≥ +{PROMOTE_IC_MARGIN}, hit ≥ −{PROMOTE_HIT_TOLERANCE})"

    log.info(f"Champion : {champion_eval}")
    log.info(f"Challenger: {challenger_eval}")
    log.info(f"Decision  : {'PROMOTE' if promoted else 'KEEP CHAMPION'} — {reason}")

    # 4. Promote (atomic-ish: challenger dir becomes the champion contents).
    if promoted:
        CHAMPION_DIR.mkdir(parents=True, exist_ok=True)
        for f in candidate_dir.iterdir():
            shutil.copy2(f, CHAMPION_DIR / f.name)

    # 5. Registry entry (append-only).
    entry = {
        "version": ts,
        "market": market,
        "run_at": datetime.now(timezone.utc).isoformat(),
        "data_window": f"{panel['date'].min().date()} → {panel['date'].max().date()}",
        "rows": len(panel),
        "test_slice_from": str(b2.date()),
        "challenger": challenger_eval,
        "champion_before": champion_eval,
        "promoted": promoted,
        "reason": reason,
        "artifact_sha256": sha256(candidate_dir / "xgb_ranking.json"),
        "winner_feature_set": challenger_metrics.get("winner"),
    }
    registry = json.loads(REGISTRY.read_text()) if REGISTRY.exists() else []
    registry.append(entry)
    REGISTRY.write_text(json.dumps(registry, indent=2))
    log.info(f"Registry updated → {REGISTRY} ({len(registry)} entries)")
    return entry


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--market", default="us")
    ap.add_argument("--period", default="10y")
    args = ap.parse_args()
    main(args.market, args.period)
