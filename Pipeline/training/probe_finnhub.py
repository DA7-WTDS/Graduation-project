# QuantWise — Finnhub historical-depth probe (MVP_PLAN § C.0).
#
# Settles the replay-corpus design assumptions with measurements instead of
# guesses: how far back /company-news actually covers, how deep the
# /stock/upgrade-downgrade ledgers go, how many monthly /recommendation
# buckets exist, and whether rate limits hold at our throttle.
#
# Read-only, ~50 calls total (throttled via the same limiter as live scoring).
# Writes training/data/replay_corpus/depth_probe.json beside where the corpus
# manifest will live.
#
# Usage:  python -m training.probe_finnhub

from __future__ import annotations

import json
import logging
import os
import statistics
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import requests

sys.path.insert(0, str(Path(__file__).parent.parent))

from markets.us.provider import FINNHUB_BASE, _finnhub_throttle  # noqa: E402
from training.eval_sentiment_llm import load_dotenv_upward                        # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

TICKERS = ["AAPL", "MSFT", "NVDA", "JPM", "UNH", "XOM", "WMT", "CAT", "AMT", "INTC"]

# Windows chosen against registry.json.test_slice_from (2024-12-31): the OOS
# era coverage question, plus one old-era window to quantify decay.
RECENT_FROM, RECENT_TO = "2024-11-20", "2025-01-10"
OLD_FROM, OLD_TO = "2019-01-01", "2019-02-01"

OUT_PATH = Path("training/data/replay_corpus/depth_probe.json")

# Verdict thresholds (documented up front so the probe can't be tuned to taste).
NEWS_RECENT_MIN = 30      # headlines/ticker over the ~7-week recent window
ACTIONS_MIN_YEARS = 3     # ledger span considered "deep"
REC_BUCKETS_MIN = 12      # monthly consensus buckets considered usable


_API_KEY: str | None = None


def _get(path: str, params: dict) -> tuple[object | None, dict]:
    """Throttled GET returning (json|None, response headers) so the probe can
    observe Finnhub's rate-limit headers directly."""
    _finnhub_throttle()
    try:
        resp = requests.get(
            f"{FINNHUB_BASE}{path}",
            params={**params, "token": _API_KEY},
            timeout=15,
        )
        headers = {k: v for k, v in resp.headers.items() if "ratelimit" in k.lower()}
        if resp.status_code != 200:
            log.warning(f"{path} {params.get('symbol')} HTTP {resp.status_code}")
            return None, headers
        return resp.json(), headers
    except Exception as e:
        log.warning(f"{path} {params.get('symbol')} failed — {e}")
        return None, {}


def probe_ticker(ticker: str, last_headers: dict) -> dict:
    result: dict = {"ticker": ticker}

    t0 = time.perf_counter()
    news_recent, result["ratelimit_headers"] = _get(
        "/company-news", {"symbol": ticker, "from": RECENT_FROM, "to": RECENT_TO})
    result["news_recent_seconds"] = round(time.perf_counter() - t0, 2)

    titles: set[str] = set()
    if isinstance(news_recent, list):
        for it in news_recent:
            h = (it.get("headline") or "").strip().lower()
            if h:
                titles.add(h)
    result["news_recent_unique"] = len(titles)

    news_old, _ = _get("/company-news", {"symbol": ticker, "from": OLD_FROM, "to": OLD_TO})
    result["news_old_jan2019_unique"] = (
        len({(it.get("headline") or "").strip().lower() for it in news_old})
        if isinstance(news_old, list) else 0
    )

    ud, _ = _get("/stock/upgrade-downgrade", {"symbol": ticker})
    if isinstance(ud, list) and ud:
        dates = sorted(str(r.get("gradeTime", "")) for r in ud if r.get("gradeTime"))
        years = [d[:4] for d in dates]
        result["actions_count"] = len(ud)
        result["actions_first_year"] = years[0]
        result["actions_last_year"] = years[-1]
        result["actions_span_years"] = int(years[-1]) - int(years[0])
    else:
        result["actions_count"] = 0

    rec, _ = _get("/stock/recommendation", {"symbol": ticker})
    if isinstance(rec, list) and rec:
        periods = sorted(str(r.get("period", "")) for r in rec if r.get("period"))
        result["rec_buckets"] = len(periods)
        result["rec_first_period"] = periods[0]
        result["rec_last_period"] = periods[-1]
    else:
        result["rec_buckets"] = 0

    pt, _ = _get("/stock/price-target", {"symbol": ticker})
    result["price_target_shape"] = (
        "dict(current-only)" if isinstance(pt, dict) else str(type(pt).__name__)
    )
    if isinstance(pt, dict):
        result["price_target_last_updated"] = pt.get("lastUpdated")

    log.info(
        f"{ticker}: news_recent={result['news_recent_unique']} "
        f"news_2019={result['news_old_jan2019_unique']} "
        f"actions={result['actions_count']} ({result.get('actions_first_year', '-')}→{result.get('actions_last_year', '-')}) "
        f"rec_buckets={result['rec_buckets']} ({result.get('rec_first_period', '-')}→{result.get('rec_last_period', '-')})"
    )
    return result


def main() -> int:
    global _API_KEY
    load_dotenv_upward()
    _API_KEY = (os.getenv("FINNHUB_API_KEY") or "").strip() or None
    if not _API_KEY:
        log.error("No FINNHUB_API_KEY in environment — set it in Pipeline/.env")
        return 1

    started = datetime.now(timezone.utc)
    results = []
    last_headers: dict = {}
    for t in TICKERS:
        results.append(probe_ticker(t, last_headers))

    def median(field: str) -> float:
        vals = [r[field] for r in results if isinstance(r.get(field), (int, float))]
        return statistics.median(vals) if vals else 0.0

    spans = [r.get("actions_span_years") for r in results if r.get("actions_span_years") is not None]
    summary = {
        "started_at": started.isoformat(),
        "tickers_probed": len(results),
        "total_calls": len(results) * 5,
        "median_news_recent_unique": median("news_recent_unique"),
        "median_news_old_2019_unique": median("news_old_jan2019_unique"),
        "median_actions_span_years": statistics.median(spans) if spans else 0,
        "median_rec_buckets": median("rec_buckets"),
        "earliest_rec_period": min((r["rec_first_period"] for r in results if r.get("rec_first_period")), default=None),
        "ratelimit_sample": results[-1].get("ratelimit_headers"),
        "verdicts": {
            "news_oos_coverage": "OK" if median("news_recent_unique") >= NEWS_RECENT_MIN else "THIN",
            "action_ledgers_deep": bool(spans) and statistics.median(spans) >= ACTIONS_MIN_YEARS,
            "consensus_usable": median("rec_buckets") >= REC_BUCKETS_MIN,
            "price_target_current_only": all(
                r.get("price_target_shape") == "dict(current-only)" for r in results),
        },
    }

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps({"summary": summary, "tickers": results}, indent=2), encoding="utf-8")

    log.info(json.dumps(summary, indent=2))
    log.info(f"Wrote {OUT_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
