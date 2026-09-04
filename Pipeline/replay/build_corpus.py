# QuantWise — replay corpus builder (MVP_PLAN § C step 1).
#
# Fetches, once, everything the point-in-time scorer will need to replay history:
# news headlines, analyst action ledgers and consensus buckets, each stamped with
# the timestamp that decides whether it was knowable at replay date t.
#
# What the probe (§ C.0) established, and what it forces here:
#
#   • /company-news caps a response at ~250 items and returns the NEWEST tail at or
#     before `to`. A single call for a wide range therefore silently truncates —
#     you get the last few weeks and no error. The fix is adaptive: fetch a slice,
#     and if it comes back at the cap, halve it and recurse. Anything else quietly
#     builds a corpus with holes in it.
#   • That endpoint retains roughly 12 months, so the DEFAULT window is the
#     news-bearing part of the out-of-sample era rather than the whole of it
#     (replay/window.py). Reaching further back only spends throttled calls on
#     empty slices — ~18 per ticker at the current boundary — and yields dates whose
#     sentiment is composed differently from every other date. The measured
#     coverage start is written to the manifest, because "how much of the window
#     has news" is a caveat the methodology page has to state precisely.
#   • Finnhub's analyst actions are premium (HTTP 403 on our key). yfinance's
#     ledger substitutes and goes back to 2012, covering the whole window.
#   • Price targets are NOT collected. Vendors expose only the CURRENT target, so
#     using one at a past date would leak the future into the backtest
#     (§ C.2 rule 3). The omission is deliberate; do not "fix" it.
#
# Resumable by design: this is thousands of throttled calls over hours, and a
# network blip must not cost the whole run. Each ticker's shard is written as it
# completes and skipped on a later invocation unless --refresh is passed.
#
# Usage:
#   python -m replay.build_corpus                      # news-bearing OOS window, whole universe
#   python -m replay.build_corpus --tickers AAPL,MSFT --refresh
#   python -m replay.build_corpus --start 2024-12-31   # full OOS era; the pre-news part
#                                                      # returns empty slices, see replay/window.py

from __future__ import annotations

import argparse
import hashlib
import json
import logging
import sys
from datetime import date, datetime, timedelta, timezone
from pathlib import Path
from typing import Any

import pandas as pd
import requests

sys.path.insert(0, str(Path(__file__).parent.parent))

from training.eval_sentiment_llm import load_dotenv_upward                  # noqa: E402

# BEFORE the provider import, not after: markets.us.provider binds FINNHUB_API_KEY at
# module scope, so importing it first leaves the key empty and every profile/consensus
# call silently returns nothing.
load_dotenv_upward()

from core.data_provider import get_provider                                    # noqa: E402
from markets.us.provider import (FINNHUB_BASE, _finnhub_profile_name,          # noqa: E402
                                 _finnhub_throttle, _yf_throttle)
from replay.window import default_corpus_window, news_horizon, oos_boundary  # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

CORPUS_DIR = Path(__file__).parent.parent / "training" / "data" / "replay_corpus"

# Finnhub truncates a /company-news response at roughly this many items. Treated
# as "the slice was too wide" rather than a hard number, so a vendor change to 200
# or 300 degrades into extra splitting instead of silent data loss.
PAGE_CAP = 240

# Stop subdividing here. A single day that still hits the cap is genuinely more
# news than the endpoint will hand over, and recursing further just burns calls.
MIN_SLICE_DAYS = 1

# Opening slice width. Wide enough that quiet tickers finish in one call, narrow
# enough that busy ones do not spend every call at the cap.
INITIAL_SLICE_DAYS = 14


def _api_key() -> str:
    import os
    key = (os.getenv("FINNHUB_API_KEY") or "").strip()
    if not key:
        raise SystemExit(
            "No FINNHUB_API_KEY in the environment. Set it in Pipeline/.env — the "
            "corpus cannot be built without it, and news older than ~12 months "
            "cannot be recovered later.")
    return key


def _news_slice(ticker: str, frm: date, to: date, key: str) -> list[dict] | None:
    """One /company-news call. None distinguishes a failed call from an empty window."""
    _finnhub_throttle()
    try:
        resp = requests.get(
            f"{FINNHUB_BASE}/company-news",
            params={"symbol": ticker, "from": frm.isoformat(), "to": to.isoformat(), "token": key},
            timeout=20,
        )
        if resp.status_code != 200:
            log.warning(f"{ticker} {frm}->{to}: HTTP {resp.status_code}")
            return None
        data = resp.json()
        return data if isinstance(data, list) else []
    except Exception as e:
        log.warning(f"{ticker} {frm}->{to}: {e}")
        return None


def fetch_news(ticker: str, start: date, end: date, key: str) -> tuple[list[dict], int]:
    """Every retained headline for `ticker` in [start, end], via adaptive slicing.

    Returns (items, call_count). Walks backwards in slices; whenever a slice comes
    back at the cap it is halved and both halves are fetched, because a capped
    response means items were dropped without saying so.
    """
    collected: dict[str, dict] = {}
    calls = 0

    def take(frm: date, to: date, width: int) -> None:
        nonlocal calls
        if frm > to:
            return
        items = _news_slice(ticker, frm, to, key)
        calls += 1
        if items is None:
            return

        if len(items) >= PAGE_CAP and (to - frm).days > MIN_SLICE_DAYS:
            mid = frm + (to - frm) / 2
            take(frm, mid, width)
            take(mid + timedelta(days=1), to, width)
            return

        for it in items:
            ts = it.get("datetime")
            headline = (it.get("headline") or "").strip()
            if not headline or not ts:
                continue
            # Dedup on (id or headline, timestamp): wires re-publish the same
            # headline, and counting it twice would overweight it in the average.
            uid = str(it.get("id") or headline.lower())
            collected.setdefault(f"{uid}|{ts}", {
                "ticker": ticker,
                "published_at": datetime.fromtimestamp(int(ts), tz=timezone.utc).isoformat(),
                "headline": headline,
                "source": it.get("source"),
                "url": it.get("url"),
            })

    cursor = end
    while cursor >= start:
        frm = max(start, cursor - timedelta(days=INITIAL_SLICE_DAYS - 1))
        take(frm, cursor, INITIAL_SLICE_DAYS)
        cursor = frm - timedelta(days=1)

    return sorted(collected.values(), key=lambda r: r["published_at"]), calls


def fetch_actions(ticker: str) -> list[dict]:
    """Analyst upgrade/downgrade ledger from yfinance (Finnhub's is premium).

    Every row carries its grade date, which is what makes it usable at a past t:
    the scorer keeps only rows at or before the replay cutoff.
    """
    try:
        _yf_throttle()
        import yfinance as yf
        ud = yf.Ticker(ticker).get_upgrades_downgrades()
    except Exception as e:
        log.warning(f"{ticker}: action ledger failed — {e}")
        return []

    if ud is None or len(ud) == 0:
        return []

    ud = ud.reset_index()
    ud.columns = [str(c).lower().replace(" ", "") for c in ud.columns]
    date_col = next((c for c in ud.columns if "date" in c or "time" in c), ud.columns[0])

    rows = []
    for _, r in ud.iterrows():
        try:
            ts = pd.to_datetime(r[date_col], utc=True)
        except Exception:
            continue
        if pd.isna(ts):
            continue
        rows.append({
            "ticker": ticker,
            "graded_at": ts.isoformat(),
            "firm": str(r.get("firm") or ""),
            "action": str(r.get("action") or "").lower(),
            "to_grade": str(r.get("tograde") or "").lower(),
            "from_grade": str(r.get("fromgrade") or "").lower(),
        })
    return sorted(rows, key=lambda r: r["graded_at"])


def fetch_consensus(ticker: str, key: str) -> list[dict]:
    """Monthly analyst-consensus buckets. Shallow (~4 months on this key), so the
    scorer treats consensus as effectively absent over most of the window — kept
    anyway because it costs one call and the depth may improve on a paid plan."""
    _finnhub_throttle()
    try:
        resp = requests.get(
            f"{FINNHUB_BASE}/stock/recommendation",
            params={"symbol": ticker, "token": key}, timeout=20)
        if resp.status_code != 200:
            return []
        data = resp.json()
    except Exception as e:
        log.warning(f"{ticker}: consensus failed — {e}")
        return []

    rows = []
    for r in data if isinstance(data, list) else []:
        period = r.get("period")
        if not period:
            continue
        rows.append({
            "ticker": ticker,
            "period": period,
            "strong_buy": int(r.get("strongBuy") or 0),
            "buy": int(r.get("buy") or 0),
            "hold": int(r.get("hold") or 0),
            "sell": int(r.get("sell") or 0),
            "strong_sell": int(r.get("strongSell") or 0),
        })
    return sorted(rows, key=lambda r: r["period"])


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()[:16]


def build(market: str, tickers: list[str], start: date, end: date, refresh: bool) -> dict:
    key = _api_key()
    CORPUS_DIR.mkdir(parents=True, exist_ok=True)
    (CORPUS_DIR / "news").mkdir(exist_ok=True)
    (CORPUS_DIR / "actions").mkdir(exist_ok=True)
    (CORPUS_DIR / "consensus").mkdir(exist_ok=True)

    per_ticker: dict[str, Any] = {}
    total_calls = 0

    for n, ticker in enumerate(tickers, 1):
        news_path = CORPUS_DIR / "news" / f"{ticker}.parquet"
        actions_path = CORPUS_DIR / "actions" / f"{ticker}.parquet"
        consensus_path = CORPUS_DIR / "consensus" / f"{ticker}.parquet"

        if news_path.exists() and not refresh:
            log.info(f"[{n}/{len(tickers)}] {ticker}: already fetched, skipping (--refresh to redo)")
            existing = pd.read_parquet(news_path)
            per_ticker[ticker] = {
                "news_rows": len(existing),
                "news_first": existing["published_at"].min() if len(existing) else None,
                "news_last": existing["published_at"].max() if len(existing) else None,
                "skipped": True,
            }
            continue

        log.info(f"[{n}/{len(tickers)}] {ticker}: fetching...")
        news, calls = fetch_news(ticker, start, end, key)
        total_calls += calls
        actions = fetch_actions(ticker)
        consensus = fetch_consensus(ticker, key)
        # The live path filters headlines for company relevance using the profile name
        # (markets/us/provider._company_keywords). Replay has to apply the same filter or
        # it scores a different headline set than live did — so the name is captured here,
        # once, rather than re-fetched per replay date.
        name = _finnhub_profile_name(ticker)
        total_calls += 1
        total_calls += 1

        pd.DataFrame(news, columns=["ticker", "published_at", "headline", "source", "url"]).to_parquet(news_path, index=False)
        pd.DataFrame(actions, columns=["ticker", "graded_at", "firm", "action", "to_grade", "from_grade"]).to_parquet(actions_path, index=False)
        pd.DataFrame(consensus, columns=["ticker", "period", "strong_buy", "buy", "hold", "sell", "strong_sell"]).to_parquet(consensus_path, index=False)

        per_ticker[ticker] = {
            "news_rows": len(news),
            "news_first": news[0]["published_at"] if news else None,
            "news_last": news[-1]["published_at"] if news else None,
            "news_calls": calls,
            "action_rows": len(actions),
            "action_first": actions[0]["graded_at"] if actions else None,
            "consensus_buckets": len(consensus),
            "company_name": name,
            "skipped": False,
        }
        log.info(
            f"    {ticker}: {len(news)} headlines ({calls} calls), "
            f"{len(actions)} actions, {len(consensus)} consensus buckets")

    # The manifest is the honesty record. It states where news actually begins per
    # ticker, so the replay engine and the methodology page quote a measured
    # coverage boundary rather than the nominal "~12 months".
    firsts = [v["news_first"] for v in per_ticker.values() if v.get("news_first")]
    manifest = {
        "built_at": datetime.now(timezone.utc).isoformat(),
        "market": market,
        "requested_window": {"start": start.isoformat(), "end": end.isoformat()},
        "tickers": len(tickers),
        "total_api_calls": total_calls,
        "news_coverage_starts": min(firsts) if firsts else None,
        "price_targets": "EXCLUDED — vendors expose current-only; using them at a past date leaks (§ C.2 rule 3)",
        "per_ticker": per_ticker,
        # Keyed by kind/name: all three shards for a ticker share a filename, so
        # keying on the bare name silently kept one hash in three.
        "shard_hashes": {
            f"{kind}/{p.name}": _sha256(p)
            for kind in ("news", "actions", "consensus")
            for p in sorted((CORPUS_DIR / kind).glob("*.parquet"))
        },
    }
    (CORPUS_DIR / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    log.info(f"Corpus written to {CORPUS_DIR}")
    log.info(f"News coverage begins {manifest['news_coverage_starts']} "
             f"(requested from {start}) — dates before that replay without a news component.")
    return manifest


def main() -> int:
    ap = argparse.ArgumentParser(description="Build the point-in-time replay corpus (MVP_PLAN § C).")
    ap.add_argument("--market", default="us")
    ap.add_argument("--start", default=None,
                    help="Defaults to the news-bearing part of the out-of-sample era: "
                         "max(registry test_slice_from, today - 365d). Earlier dates fetch "
                         "empty slices because Finnhub does not retain them.")
    ap.add_argument("--end", default=None, help="Defaults to today (UTC).")
    ap.add_argument("--tickers", default=None, help="Comma-separated override; defaults to the live universe.")
    ap.add_argument("--refresh", action="store_true", help="Re-fetch tickers that already have a shard.")
    args = ap.parse_args()

    end = date.fromisoformat(args.end) if args.end else datetime.now(timezone.utc).date()
    default_start, _ = default_corpus_window(today=end)
    start = date.fromisoformat(args.start) if args.start else default_start

    if start < default_start:
        wasted = (default_start - start).days
        log.warning(
            f"--start {start} reaches back past what is fetchable: the news horizon is "
            f"{news_horizon(end)} and the out-of-sample boundary is {oos_boundary()}. "
            f"Roughly {wasted} days will return empty slices ({wasted // INITIAL_SLICE_DAYS} "
            f"wasted calls per ticker).")

    if args.tickers:
        tickers = [t.strip().upper() for t in args.tickers.split(",") if t.strip()]
    else:
        tickers = get_provider(args.market).get_universe()

    log.info(f"Corpus: {len(tickers)} tickers, {start} -> {end}")
    build(args.market, tickers, start, end, args.refresh)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
