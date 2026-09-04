# QuantWise — point-in-time scorer, the fast lane (MVP_PLAN § C step 2).
#
# Replays the daily run over history: for each date t it reconstructs what the
# pipeline would have seen at t and scores it through the same champion, the same
# sentiment composite and the same risk rules as live.
#
# THIS IS THE RESEARCH LANE, NOT THE PUBLISHED NUMBER (§ C.2 rule 4). It produces
# ScoreRecord-shaped rows so the fidelity lane can feed them through the real .NET
# optimizer and shadow jobs; a portfolio result computed here in Python would be a
# reimplementation of the product rather than a measurement of it.
#
# Where leakage would come from, and what stops it:
#
#   • Prices — features are computed from the OHLCV frame truncated at t. Every
#     indicator is backward-looking by construction (core/features.py), so the
#     truncation is the whole guard.
#   • Cutoff — the as-of instant is t+1 at 01:00 UTC, matching the live post-close
#     cron, NOT midnight on t (§ C.2 rule 2). A naive midnight cutoff would discard
#     the after-close news that the live run genuinely did see.
#   • News/actions — filtered to `published_at <= cutoff` against the corpus. The
#     corpus holds everything to today, so this filter is the only thing between
#     the replay and tomorrow's headlines.
#   • Price targets — never loaded. Vendors expose only the current value.
#   • Universe — today's constituents, so this inherits survivorship bias. Stated
#     in the output manifest; fixed only by point-in-time constituents.
#
# Usage:
#   python -m replay.score_asof --start 2025-09-01 --end 2026-06-01
#   python -m replay.score_asof --start 2026-08-01 --tickers AAPL,MSFT --no-finbert

from __future__ import annotations

import argparse
import json
import logging
import sys
from datetime import date, datetime, time, timedelta, timezone
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd

sys.path.insert(0, str(Path(__file__).parent.parent))

from core import sentiment_scoring as ss                                       # noqa: E402
from core.analyst_actions import ActionRow, score_actions                      # noqa: E402
from core.data_provider import get_provider                                    # noqa: E402
from core.features import compute_features                                     # noqa: E402
from markets.us.provider import (NEWS_LIMIT, NEWS_MIN_RELEVANT,                # noqa: E402
                                 _company_keywords, _filter_relevant, _rating_label)
from risk_rules import apply_risk_rules                                        # noqa: E402

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

BASE_DIR = Path(__file__).parent.parent
CORPUS_DIR = BASE_DIR / "training" / "data" / "replay_corpus"
OUT_DIR = BASE_DIR / "training" / "data" / "replay_runs"
MODEL_DIR = BASE_DIR / "models" / "ranking_v1"

# The live run fires at 01:00 UTC the morning after the session it scores, so a
# replayed date must see the same after-close window (§ C.2 rule 2).
CUTOFF_HOUR_UTC = 1

# Live news window: markets/us/provider.FINNHUB_NEWS_DAYS.
NEWS_WINDOW_DAYS = 14

# Enough history for the 60-day look-back plus indicator warmup, matching
# main.MIN_RAW_ROWS. Below this a date is skipped rather than scored on thin data.
MIN_RAW_ROWS = 120


def as_of_cutoff(d: date) -> datetime:
    """The instant a live run scoring session `d` would have had its data by."""
    return datetime.combine(d + timedelta(days=1), time(CUTOFF_HOUR_UTC), tzinfo=timezone.utc)


class Corpus:
    """The replay corpus, sliced point-in-time.

    Everything is loaded once and filtered per date in memory: the alternative is
    re-reading parquet per (ticker, date), which for a 600-day window over 100
    tickers is 60,000 reads of the same files.
    """

    def __init__(self, corpus_dir: Path = CORPUS_DIR):
        self.dir = corpus_dir
        manifest_path = corpus_dir / "manifest.json"
        if not manifest_path.exists():
            raise SystemExit(
                f"No corpus manifest at {manifest_path}. Run `python -m replay.build_corpus` first — "
                "the scorer cannot invent history that was never fetched.")
        self.manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        self.news: dict[str, pd.DataFrame] = {}
        self.actions: dict[str, list[ActionRow]] = {}
        self.consensus: dict[str, pd.DataFrame] = {}

    def load(self, tickers: list[str]) -> list[str]:
        """Load shards; returns the tickers that actually have a news shard."""
        available = []
        for t in tickers:
            npath = self.dir / "news" / f"{t}.parquet"
            if not npath.exists():
                continue
            available.append(t)

            frame = pd.read_parquet(npath)
            if len(frame):
                frame["ts"] = pd.to_datetime(frame["published_at"], utc=True)
            self.news[t] = frame

            apath = self.dir / "actions" / f"{t}.parquet"
            if apath.exists():
                af = pd.read_parquet(apath)
                self.actions[t] = [
                    ActionRow(
                        graded_at=pd.to_datetime(r["graded_at"], utc=True).to_pydatetime(),
                        action=r["action"] or "",
                        to_grade=r["to_grade"] or "",
                        firm=(r["firm"] or None),
                    )
                    for _, r in af.iterrows()
                ]
            else:
                self.actions[t] = []

            cpath = self.dir / "consensus" / f"{t}.parquet"
            self.consensus[t] = pd.read_parquet(cpath) if cpath.exists() else pd.DataFrame()
        return available

    def company_name(self, ticker: str) -> str:
        return (self.manifest.get("per_ticker", {}).get(ticker, {}) or {}).get("company_name") or ""

    def headlines(self, ticker: str, cutoff: datetime) -> list[str]:
        """Headlines in the live 14-day window ending at `cutoff`, filtered for
        company relevance exactly as the live path filters them."""
        frame = self.news.get(ticker)
        if frame is None or frame.empty:
            return []
        window_start = cutoff - timedelta(days=NEWS_WINDOW_DAYS)
        rows = frame[(frame["ts"] <= cutoff) & (frame["ts"] > window_start)]
        if rows.empty:
            return []

        titles = list(dict.fromkeys(rows.sort_values("ts", ascending=False)["headline"].tolist()))
        relevant = _filter_relevant(titles, _company_keywords(ticker, self.company_name(ticker)))
        # Live drops the news component entirely below this floor rather than
        # scoring on one stray headline; replay must do the same or it scores news
        # on days live would not have.
        if len(relevant) < NEWS_MIN_RELEVANT:
            return []
        return relevant[:NEWS_LIMIT]

    def action_rows(self, ticker: str) -> list[ActionRow]:
        return self.actions.get(ticker, [])

    def consensus_at(self, ticker: str, cutoff: datetime) -> tuple[float | None, str | None, int]:
        """Latest monthly consensus bucket at or before the cutoff.

        Shallow on the free tier (~4 months), so for most replayed dates this
        correctly returns nothing and the composite reweights without it.
        """
        frame = self.consensus.get(ticker)
        if frame is None or frame.empty:
            return None, None, 0
        month = cutoff.date().replace(day=1).isoformat()
        eligible = frame[frame["period"] <= month]
        if eligible.empty:
            return None, None, 0
        row = eligible.sort_values("period").iloc[-1]
        sb, b, h, s, sell = (float(row["strong_buy"]), float(row["buy"]), float(row["hold"]),
                             float(row["sell"]), float(row["strong_sell"]))
        n = sb + b + h + s + sell
        if n <= 0:
            return None, None, 0
        avg = (5 * sb + 4 * b + 3 * h + 2 * s + 1 * sell) / n
        return round(avg, 2), _rating_label(avg), int(n)


class FinbertCache:
    """Scores each unique headline once.

    The same headline appears in every window it falls into, so scoring per
    (ticker, date) would run the model tens of times on identical text. One pass
    over the deduplicated set makes an overnight CPU run feasible (§ C.3).
    """

    def __init__(self, enabled: bool = True):
        self.scores: dict[str, float] = {}
        self.model = None
        if enabled:
            try:
                from transformers import pipeline as hf_pipeline
                self.model = hf_pipeline("text-classification", model="ProsusAI/finbert", top_k=None)
                log.info("FinBERT loaded for replay scoring.")
            except Exception as e:
                log.error(f"FinBERT unavailable, replay will run without a news component — {e}")

    def warm(self, headlines: set[str], batch_size: int = 32) -> None:
        if self.model is None or not headlines:
            return
        todo = sorted(h for h in headlines if h not in self.scores)
        if not todo:
            return
        log.info(f"FinBERT: scoring {len(todo)} unique headlines...")
        for i in range(0, len(todo), 512):
            chunk = todo[i:i + 512]
            outs = self.model(chunk, truncation=True, max_length=128, batch_size=batch_size)
            for headline, out in zip(chunk, outs):
                probs = {x["label"].lower(): x["score"] for x in out}
                self.scores[headline] = probs.get("positive", 0.0) - probs.get("negative", 0.0)
            log.info(f"    {min(i + 512, len(todo))}/{len(todo)}")

    def score(self, headlines: list[str]) -> float | None:
        vals = [self.scores[h] for h in headlines if h in self.scores]
        if not vals:
            return None
        return round(float(np.mean(vals)), 3)


def trading_days(frames: dict[str, pd.DataFrame], start: date, end: date) -> list[date]:
    """Session dates taken from the price data itself, so holidays and closures
    come from the exchange's actual behaviour rather than a hard-coded calendar."""
    seen: set[date] = set()
    for frame in frames.values():
        if frame is None or frame.empty:
            continue
        for d in pd.to_datetime(frame["date"]).dt.date:
            if start <= d <= end:
                seen.add(d)
    return sorted(seen)


def _prepare_frames(provider, tickers: list[str], period: str) -> dict[str, pd.DataFrame]:
    """One bulk OHLCV download, normalized to lowercase date/OHLCV columns."""
    raw = provider.get_ohlcv_batch(tickers, period=period)
    if raw is None or raw.empty:
        raise SystemExit("OHLCV download returned nothing.")
    is_multi = hasattr(raw.columns, "levels")

    out: dict[str, pd.DataFrame] = {}
    for t in tickers:
        try:
            frame = raw[t].dropna(how="all") if is_multi else raw.dropna(how="all")
        except Exception:
            continue
        if frame is None or frame.empty:
            continue
        df = frame.reset_index()
        df.columns = [str(c).lower() for c in df.columns]
        date_col = next((c for c in df.columns if "date" in c), None)
        if date_col is None:
            continue
        df = df.rename(columns={date_col: "date"})
        df = df[["date", "open", "high", "low", "close", "volume"]].dropna(subset=["close"])
        df["date"] = pd.to_datetime(df["date"]).dt.tz_localize(None)
        out[t] = df.sort_values("date").reset_index(drop=True)
    return out


def score_day(
    d: date,
    frames: dict[str, pd.DataFrame],
    corpus: Corpus,
    finbert: FinbertCache,
    model,
    calibrator,
    ranking_cols: list[str],
) -> list[dict[str, Any]]:
    """One replayed daily run. Returns risk-graded, ScoreRecord-shaped rows."""
    cutoff = as_of_cutoff(d)

    predictions: list[dict[str, Any]] = []
    for ticker, frame in frames.items():
        history = frame[frame["date"].dt.date <= d]
        if len(history) < MIN_RAW_ROWS:
            continue
        feat = compute_features(history.copy())
        if feat.empty:
            continue

        try:
            row = feat[ranking_cols].iloc[-1].to_numpy(dtype=np.float64)
        except KeyError:
            continue
        if not np.isfinite(row).all():
            continue

        score = float(model.predict(row.reshape(1, -1))[0])
        prob = float(np.clip(calibrator.predict(np.array([score]))[0], 0.0, 1.0))

        closes = feat["close"].astype(float)
        sma50 = float(closes.rolling(50).mean().iloc[-1]) if len(closes) >= 50 else None
        predictions.append({
            "ticker": ticker,
            "direction": "UP" if score > 0 else "DOWN",
            "change_pct": round(score * 100, 4),
            "confidence": round(prob, 4),
            "predicted_at": cutoff.isoformat(),
            "rsi_14": round(float(feat["RSI"].iloc[-1]), 2),
            "pct_vs_sma50": (round(float(closes.iloc[-1]) / sma50 - 1.0, 4)
                             if sma50 and sma50 > 0 else None),
        })

    if not predictions:
        return []

    sentiments: list[dict[str, Any]] = []
    for p in predictions:
        ticker = p["ticker"]
        headlines = corpus.headlines(ticker, cutoff)
        news = finbert.score(headlines) if headlines else None
        actions = score_actions(corpus.action_rows(ticker), cutoff)
        avg, rating_label, n_analysts = corpus.consensus_at(ticker, cutoff)

        # price_target is deliberately absent — see § C.2 rule 3. The composite
        # reweights over what is present rather than scoring the gap as neutral.
        score, signal, parts = ss.composite(
            consensus=ss.consensus_score(avg),
            actions=actions.action_score,
            news=news,
        )
        sentiments.append({
            "ticker": ticker,
            "sentiment_score": score,
            "signal": signal,
            "analyst_rating": avg,
            "rating_label": rating_label,
            "ratings_count": n_analysts,
            "recent_action": actions.latest_action,
            "recent_action_firm": actions.latest_firm,
            "recent_actions_count": actions.recent_count,
            "days_since_latest": actions.days_since_latest,
            "pt_current": None,
            "pt_mean": None,
            "pt_upside_pct": None,
            "news_score": news,
            "news_label": ss.label(news) if news is not None else None,
            "news_count": len(headlines),
            "components": parts,
            "analyzed_at": cutoff.isoformat(),
        })

    try:
        return apply_risk_rules(predictions, sentiments)
    except ValueError as e:
        # The live MIN_RECORDS guard. A thin day is dropped rather than published.
        log.warning(f"{d}: {e}")
        return []


def main() -> int:
    ap = argparse.ArgumentParser(description="Point-in-time replay scorer (MVP_PLAN § C, fast lane).")
    ap.add_argument("--start", required=True)
    ap.add_argument("--end", default=None, help="Defaults to today (UTC).")
    ap.add_argument("--market", default="us")
    ap.add_argument("--tickers", default=None, help="Comma-separated override.")
    ap.add_argument("--period", default="5y", help="OHLCV history to download (needs warmup before --start).")
    ap.add_argument("--no-finbert", action="store_true", help="Skip the news component (fast smoke runs).")
    ap.add_argument("--out", default=None)
    args = ap.parse_args()

    start = date.fromisoformat(args.start)
    end = date.fromisoformat(args.end) if args.end else datetime.now(timezone.utc).date()
    out_dir = Path(args.out) if args.out else OUT_DIR
    out_dir.mkdir(parents=True, exist_ok=True)

    import pickle
    import xgboost as xgb

    ranking_cols = json.loads((MODEL_DIR / "features.json").read_text(encoding="utf-8"))
    model = xgb.XGBRegressor()
    model.load_model(MODEL_DIR / "xgb_ranking.json")
    with open(MODEL_DIR / "calibrator.pkl", "rb") as fh:
        calibrator = pickle.load(fh)

    corpus = Corpus()
    provider = get_provider(args.market)
    tickers = ([t.strip().upper() for t in args.tickers.split(",")] if args.tickers
               else provider.get_universe())
    available = corpus.load(tickers)
    if not available:
        raise SystemExit("No corpus shards for these tickers. Run replay.build_corpus first.")
    log.info(f"Corpus covers {len(available)}/{len(tickers)} requested tickers.")

    log.info(f"Downloading {args.period} of OHLCV...")
    frames = _prepare_frames(provider, available, args.period)
    days = trading_days(frames, start, end)
    log.info(f"{len(days)} trading days to replay: {days[0]} -> {days[-1]}" if days else "no trading days")
    if not days:
        return 1

    # Warm FinBERT once over every headline any window will ask for, rather than
    # per date — the same headline sits in up to 14 consecutive windows.
    finbert = FinbertCache(enabled=not args.no_finbert)
    if finbert.model is not None:
        wanted: set[str] = set()
        for d in days:
            cut = as_of_cutoff(d)
            for t in available:
                wanted.update(corpus.headlines(t, cut))
        finbert.warm(wanted)

    written = 0
    for n, d in enumerate(days, 1):
        rows = score_day(d, frames, corpus, finbert, model, calibrator, ranking_cols)
        if not rows:
            continue
        frame = pd.DataFrame(rows)
        frame["replay_date"] = d.isoformat()
        frame["as_of"] = as_of_cutoff(d).isoformat()
        partition = out_dir / f"date={d.isoformat()}"
        partition.mkdir(parents=True, exist_ok=True)
        frame.to_parquet(partition / "part-0.parquet", index=False)
        written += 1
        if n % 20 == 0 or n == len(days):
            log.info(f"    {n}/{len(days)} days ({written} written)")

    manifest = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "lane": "fast (research) — NOT the published number; see MVP_PLAN § C.2 rule 4",
        "window": {"start": start.isoformat(), "end": end.isoformat()},
        "days_replayed": written,
        "tickers": len(available),
        "cutoff_convention": f"t+1 {CUTOFF_HOUR_UTC:02d}:00 UTC, matching the live post-close run",
        "news_component": "disabled" if finbert.model is None else "FinBERT over corpus headlines",
        "news_coverage_starts": corpus.manifest.get("news_coverage_starts"),
        "excluded": {
            "price_targets": "current-only at source; using them at a past date would leak",
        },
        "known_bias": "universe is today's constituents (survivorship); fixed only by point-in-time constituents",
    }
    (out_dir / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    log.info(f"Replay written to {out_dir} ({written} days).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
