# QuantWise — Phase 1.2: the LSTM keep-or-kill experiment (decision D8).
#
# Question: does the frozen deployed LSTM's 64-dim temporal embedding add
# measurable rank skill on the NEW relative target, over XGBoost on the same
# 14 indicators alone?
#
# Method (fair by construction):
#   • Embeddings from the deployed backbone (models/lstm_backbone.pth), frozen,
#     eval mode, over 60-day windows of the 5 sequential features scaled with
#     the deployed global_feature_scaler — exactly the serving path, minus
#     MC-dropout (point embeddings suffice for ranking).
#   • Both variants train and evaluate on the IDENTICAL row subset (rows with
#     a full 60-day look-back), identical chronological split + purge gap,
#     identical XGBoost hyperparameters.
#
# DECISION RULE (pre-agreed in IMPLEMENTATION_PLAN § 1.2):
#   keep the hybrid only if  ΔIC ≥ +0.02  or  Δhit-rate ≥ +1.5pp  on test.
#   Otherwise ship trees-only: cheaper serving, no torch, simpler retraining.
#
# Usage:
#   python -m training.experiment_lstm --data training/data/us_ranking.pkl --out models/ranking_v1

from __future__ import annotations

import argparse
import json
import logging
import pickle
import sys
from pathlib import Path

import numpy as np
import pandas as pd
import torch
import xgboost as xgb

sys.path.insert(0, str(Path(__file__).parent.parent))

from core.lstm import LSTMBackbone                                  # noqa: E402
from training.build_dataset import SEQ_COLS, TECH_COLS              # noqa: E402
from training.train_ranking import PURGE_CALENDAR_DAYS, chrono_split, evaluate  # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

torch.backends.mkldnn.enabled = False  # match serving determinism constraint

MODEL_DIR = Path(__file__).parent.parent / "models"
LOOK_BACK = 60
BATCH = 4096

XGB_PARAMS = dict(
    n_estimators=600, learning_rate=0.03, max_depth=5, subsample=0.8,
    colsample_bytree=0.8, min_child_weight=20, objective="reg:squarederror",
    early_stopping_rounds=50, random_state=42, n_jobs=-1,
)


def compute_embeddings(panel: pd.DataFrame) -> tuple[np.ndarray, np.ndarray]:
    """64-dim frozen-LSTM embedding per eligible row (those with a full
    60-row look-back within their ticker). Returns (row_indices, embeddings)."""
    with open(MODEL_DIR / "universal_config.json") as f:
        cfg = json.load(f)
    lstm = LSTMBackbone(
        input_dim=cfg["lstm_params"]["input_dim"],
        hidden_dim=cfg["lstm_params"]["hidden_dim"],
        num_layers=cfg["lstm_params"]["layers"],
    )
    lstm.load_state_dict(torch.load(MODEL_DIR / "lstm_backbone.pth", map_location="cpu", weights_only=True))
    lstm.eval()
    with open(MODEL_DIR / "global_feature_scaler.pkl", "rb") as f:
        scaler = pickle.load(f)

    idx_out: list[np.ndarray] = []
    win_out: list[np.ndarray] = []

    for _, g in panel.groupby("ticker", sort=False):
        if len(g) <= LOOK_BACK:
            continue
        scaled = scaler.transform(g[SEQ_COLS].values).astype(np.float32)
        # windows[i] ends at row i+LOOK_BACK-1 → embedding for that row
        windows = np.lib.stride_tricks.sliding_window_view(scaled, (LOOK_BACK, len(SEQ_COLS)))
        windows = windows.squeeze(axis=1)                      # (n-59, 60, 5)
        idx_out.append(g.index.values[LOOK_BACK - 1:])
        win_out.append(windows)

    all_idx = np.concatenate(idx_out)
    all_win = np.concatenate(win_out)
    log.info(f"Eligible rows with full look-back: {len(all_idx):,} · running frozen LSTM...")

    embs = np.empty((len(all_win), 64), dtype=np.float32)
    with torch.no_grad():
        for s in range(0, len(all_win), BATCH):
            x = torch.from_numpy(all_win[s:s + BATCH])
            _, features = lstm(x)
            embs[s:s + BATCH] = features.numpy()
    return all_idx, embs


def fit_eval(train, val, test, cols: list[str], label: str) -> dict:
    model = xgb.XGBRegressor(**XGB_PARAMS)
    model.fit(train[cols], train["rel_return"], eval_set=[(val[cols], val["rel_return"])], verbose=False)
    test = test.copy()
    test["pred"] = model.predict(test[cols])
    m = evaluate(test, "pred")
    m["best_iteration"] = int(model.best_iteration)
    log.info(f"{label}: IC {m['ic_mean']} (t {m['ic_t_stat']}) · hit {m['hit_rate']} · spread {m['decile_spread_mean']}")
    return m


def main(data_path: Path, out_dir: Path) -> dict:
    panel: pd.DataFrame = pd.read_pickle(data_path)
    panel = panel.sort_values(["ticker", "date"]).reset_index(drop=True)

    idx, embs = compute_embeddings(panel)
    emb_cols = [f"emb_{i}" for i in range(embs.shape[1])]
    emb_df = pd.DataFrame(embs, index=idx, columns=emb_cols)
    panel = panel.join(emb_df, how="inner")  # identical row subset for BOTH variants
    log.info(f"Experiment rows (with embeddings): {len(panel):,}")

    b1, b2 = chrono_split(panel["date"])
    purge = pd.Timedelta(days=PURGE_CALENDAR_DAYS)
    train = panel[panel["date"] < b1 - purge]
    val   = panel[(panel["date"] >= b1) & (panel["date"] < b2 - purge)]
    test  = panel[panel["date"] >= b2]
    log.info(f"Split: train {len(train):,} · val {len(val):,} · test {len(test):,} (≥{b2.date()})")

    trees_only = fit_eval(train, val, test, TECH_COLS, "trees-only  ")
    hybrid     = fit_eval(train, val, test, TECH_COLS + emb_cols, "hybrid+LSTM ")

    delta_ic  = round(hybrid["ic_mean"] - trees_only["ic_mean"], 4)
    delta_hit = round(hybrid["hit_rate"] - trees_only["hit_rate"], 4)
    keep_lstm = (delta_ic >= 0.02) or (delta_hit >= 0.015)

    result = {
        "decision_rule": "keep hybrid iff dIC >= +0.02 or dHit >= +1.5pp (IMPLEMENTATION_PLAN § 1.2)",
        "trees_only": trees_only,
        "hybrid_lstm": hybrid,
        "delta_ic": delta_ic,
        "delta_hit_rate": delta_hit,
        "verdict": "KEEP LSTM (hybrid)" if keep_lstm else "DROP LSTM (ship trees-only)",
        "n_rows": len(panel),
        "test_window": f"{test['date'].min().date()} → {test['date'].max().date()}",
    }

    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "lstm_experiment.json").write_text(json.dumps(result, indent=2))
    log.info(json.dumps(result, indent=2))
    return result


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default="training/data/us_ranking.pkl")
    ap.add_argument("--out", default="models/ranking_v1")
    args = ap.parse_args()
    main(Path(args.data), Path(args.out))
