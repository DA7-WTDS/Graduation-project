# QuantWise — replay ingest driver (MVP_PLAN § C, fidelity lane).
#
# Feeds the replayed daily runs into the real backend, one date at a time, so the
# track record is produced by the actual optimizer and shadow jobs rather than by
# a Python reimplementation of them (§ C.2 rule 4). The fast lane answers "what
# did the model say"; only this answers "what would the product have done".
#
# Each replayed date is pushed as a pair:
#
#   1. POST /api/internal/instrument-stats  — vol, ADV and close AS OF that date
#   2. POST /api/internal/daily-results     — the run's records, simulated=true
#
# The stats come first and they are the reason this driver exists rather than a
# shell loop. The optimizer weights every core position by score / realized_vol,
# caps sectors, and gates the tactical sleeve on average daily value traded. Left
# to the nightly registry those three numbers are TODAY's, so replaying a 2025
# portfolio would weight it by 2026 volatility — lookahead, and not a small one:
# ~23% of the replayed universe shifted vol by more than 30% across the window,
# the largest by 1.5-2.2x. Whether that flatters or penalises depends on the
# regime, which is exactly why it cannot be waved through.
#
# Sector is NOT point-in-time: no historical GICS mapping exists on free data.
# Classification is near-static, so this is disclosed rather than solved.
#
# Idempotent. The backend keys a run on (generated_at, market, simulated), so
# re-running is safe and resumes rather than duplicating.
#
# Usage:
#   python -m replay.ingest_runs --backend http://localhost:5099
#   python -m replay.ingest_runs --from 2026-01-01 --dry-run

from __future__ import annotations

import argparse
import json
import logging
import math
import os
import sys
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any

import pandas as pd
import requests

sys.path.insert(0, str(Path(__file__).parent.parent))

from training.eval_sentiment_llm import load_dotenv_upward                      # noqa: E402

load_dotenv_upward()

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

BASE_DIR = Path(__file__).parent.parent
RUNS_DIR = BASE_DIR / "training" / "data" / "replay_runs"

# The market tag replayed runs carry. Same market as live — provenance is the
# Simulated status, not a fake market name, so EGX does not later need an
# "egx_sim" twin and nothing can be served by mistyping a market string.
MARKET = "us"

# Columns the backend's PredictionRecordDto expects, snake_case on the wire.
RECORD_FIELDS = [
    "ticker", "direction", "change_pct", "confidence", "sentiment_score", "signal",
    "analyst_rating", "rating_label", "pt_upside_pct", "news_score", "agreement",
    "risk_level", "conviction_score", "risk_flags", "rationale", "rsi_14",
    "pct_vs_sma50", "features", "model_version", "scaler_hash",
]

REQUIRED_STRINGS = {"ticker", "direction", "signal", "agreement", "risk_level", "rationale"}


def _clean(value: Any) -> Any:
    """JSON-safe. NaN is null, not the string 'NaN': pandas yields NaN for every
    absent numeric and json.dumps would otherwise emit invalid JSON that the
    backend rejects for the whole date."""
    if value is None:
        return None
    if isinstance(value, float) and math.isnan(value):
        return None
    if isinstance(value, (pd.Timestamp, datetime, date)):
        return value.isoformat()
    if hasattr(value, "tolist"):
        return value.tolist()
    return value


def to_record(row: pd.Series) -> dict[str, Any]:
    out: dict[str, Any] = {}
    for field in RECORD_FIELDS:
        value = _clean(row.get(field))
        if field == "features" and isinstance(value, str):
            try:
                value = json.loads(value)
            except (TypeError, ValueError):
                value = None
        if field in REQUIRED_STRINGS and value is None:
            value = ""
        if field == "risk_flags" and value is None:
            value = []
        out[field] = value
    return out


def load_days(runs_dir: Path, since: date | None, until: date | None) -> list[tuple[date, pd.DataFrame]]:
    days = []
    for partition in sorted(runs_dir.glob("date=*")):
        try:
            d = date.fromisoformat(partition.name.removeprefix("date="))
        except ValueError:
            continue
        if (since and d < since) or (until and d > until):
            continue
        files = sorted(partition.glob("*.parquet"))
        if not files:
            continue
        days.append((d, pd.concat([pd.read_parquet(f) for f in files], ignore_index=True)))
    return days


class Backend:
    def __init__(self, base_url: str, key: str, timeout: int = 300):
        self.base = base_url.rstrip("/")
        self.key = key
        self.timeout = timeout

    def _post(self, path: str, payload: dict) -> requests.Response:
        return requests.post(
            f"{self.base}{path}",
            json=payload,
            headers={"X-Pipeline-Key": self.key, "Content-Type": "application/json"},
            timeout=self.timeout,
        )

    def push_stats(self, as_of: date, tickers: list[str]) -> bool:
        """Point-in-time registry stats for `as_of`. Must land BEFORE the run:
        the optimizer reads the registry when it builds that date's portfolio."""
        resp = self._post("/api/internal/instrument-stats",
                          {"market": MARKET, "as_of": as_of.isoformat(), "tickers": tickers})
        if resp.status_code >= 400:
            log.error(f"{as_of}: stats push failed HTTP {resp.status_code} — {resp.text[:200]}")
            return False
        return True

    def push_run(self, generated_at: str, records: list[dict]) -> bool:
        resp = self._post("/api/internal/daily-results", {
            "generated_at": generated_at,
            "count": len(records),
            "records": records,
            "status": "ok",
            "gate_failures": [],
            "market": MARKET,
            "simulated": True,
        })
        if resp.status_code >= 400:
            log.error(f"run push failed HTTP {resp.status_code} — {resp.text[:300]}")
            return False
        return True


def main() -> int:
    ap = argparse.ArgumentParser(description="Feed replayed runs into the backend (MVP_PLAN § C).")
    ap.add_argument("--backend", default=os.getenv("BACKEND_URL", "http://localhost:5099"))
    ap.add_argument("--runs", default=str(RUNS_DIR))
    ap.add_argument("--from", dest="since", default=None, help="ISO date; default = earliest replayed day.")
    ap.add_argument("--until", default=None)
    ap.add_argument("--dry-run", action="store_true", help="Build and validate payloads without posting.")
    ap.add_argument("--skip-stats", action="store_true",
                    help="Do not push point-in-time stats. The replay then weights positions by "
                         "TODAY's volatility, which is lookahead — diagnostics only.")
    args = ap.parse_args()

    key = (os.getenv("REPLAY_INGEST_KEY") or os.getenv("Recommendations__Ingest__ApiKey") or "").strip()
    if not key and not args.dry_run:
        raise SystemExit(
            "No ingest key. Set REPLAY_INGEST_KEY (preferred: a credential distinct from the live "
            "pipeline's) or Recommendations__Ingest__ApiKey in the environment.")

    days = load_days(
        Path(args.runs),
        date.fromisoformat(args.since) if args.since else None,
        date.fromisoformat(args.until) if args.until else None,
    )
    if not days:
        raise SystemExit(f"No replayed days under {args.runs}. Run `python -m replay.score_asof` first.")

    log.info(f"{len(days)} replayed days: {days[0][0]} -> {days[-1][0]}")
    if args.skip_stats:
        log.warning("--skip-stats: positions will be weighted by TODAY's vol/ADV. Lookahead. Diagnostics only.")

    backend = Backend(args.backend, key)
    pushed = failed = 0

    for n, (d, frame) in enumerate(days, 1):
        records = [to_record(r) for _, r in frame.iterrows()]
        # The run's timestamp is the as-of cutoff the scorer stamped on it (t+1
        # 01:00 UTC), so a replayed run is keyed by the instant a live run would
        # have had its data — not by when the backfill happened to be executed.
        generated_at = str(frame["as_of"].iloc[0])

        if args.dry_run:
            if n == 1:
                log.info(f"dry-run sample ({d}, {len(records)} records): "
                         f"{json.dumps(records[0], default=str)[:300]}...")
            pushed += 1
            continue

        if not args.skip_stats:
            tickers = sorted({r["ticker"] for r in records if r["ticker"]})
            if not backend.push_stats(d, tickers):
                failed += 1
                continue

        if backend.push_run(generated_at, records):
            pushed += 1
        else:
            failed += 1
            log.error(f"{d}: run rejected; stopping so the gap is not buried under later dates.")
            break

        if n % 20 == 0 or n == len(days):
            log.info(f"    {n}/{len(days)} days ({pushed} pushed, {failed} failed)")

    log.info(f"Done. {pushed} pushed, {failed} failed.")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
