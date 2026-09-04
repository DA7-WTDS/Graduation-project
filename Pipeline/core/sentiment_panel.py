# QuantWise — the daily sentiment panel (MVP_PLAN § B clock 1, feeds § D).
#
# Appends one row per (ticker, date) for the WHOLE universe, every run, forever.
#
# Why this file exists at all: sentiment history cannot be bought retroactively.
# Finnhub keeps roughly 12 months of company news (measured — see § C.0) and analyst
# consensus only ~4 monthly buckets, so any sentiment feature older than that horizon
# is unrecoverable no matter what we pay later. Every day this does not run is a day
# permanently missing from the training panel. That is the whole argument for writing
# it before there is a model that uses it.
#
# Why the whole universe and not the scored shortlist: the pipeline used to gather
# sentiment only for the top 35 candidates by predicted return. A panel built from
# that would be conditioned on the model's own output — the sample would contain only
# names the model already liked, and any feature learned from it would be measuring
# the selection as much as the sentiment. Unbiased coverage now is what makes the
# § D A/B answerable at all.
#
# Storage: hive-partitioned Parquet, one file per run date, never rewritten. Appending
# to a single Parquet file means rewriting it, which turns every run into a chance to
# corrupt the whole history; a new partition per day cannot damage yesterday.
# pandas/pyarrow read the whole thing back with one read_parquet() on the root.

from __future__ import annotations

import logging
import os
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any

log = logging.getLogger(__name__)

BASE_DIR = Path(__file__).parent.parent

# Overridable so the container can point this at a mounted volume. A panel written
# inside an ephemeral container filesystem is worse than none: it looks like the clock
# is running while every restart silently resets it.
PANEL_DIR = Path(os.getenv("SENTIMENT_PANEL_DIR") or (BASE_DIR / "training" / "data" / "sentiment_us"))

# Columns are the raw components, not a composite. The composite's weights are a
# serving decision that will change; the parts it was built from are the facts, and a
# stored composite could not be re-derived under different weights later.
COLUMNS = [
    "date", "ticker", "as_of",
    "sentiment_score", "signal",
    "analyst_rating", "rating_label", "ratings_count",
    "recent_action", "recent_action_firm", "recent_actions_count", "days_since_latest",
    "pt_current", "pt_mean", "pt_upside_pct",
    "news_score", "news_label", "news_count",
    "component_consensus", "component_actions", "component_price_target", "component_news",
]


def _row(s: dict[str, Any], run_date: date, as_of: str) -> dict[str, Any]:
    components = s.get("components") or {}
    return {
        "date": run_date.isoformat(),
        "ticker": s.get("ticker"),
        "as_of": as_of,
        "sentiment_score": s.get("sentiment_score"),
        "signal": s.get("signal"),
        "analyst_rating": s.get("analyst_rating"),
        "rating_label": s.get("rating_label"),
        "ratings_count": s.get("ratings_count"),
        "recent_action": s.get("recent_action"),
        "recent_action_firm": s.get("recent_action_firm"),
        "recent_actions_count": s.get("recent_actions_count"),
        "days_since_latest": s.get("days_since_latest"),
        "pt_current": s.get("pt_current"),
        "pt_mean": s.get("pt_mean"),
        "pt_upside_pct": s.get("pt_upside_pct"),
        "news_score": s.get("news_score"),
        "news_label": s.get("news_label"),
        "news_count": s.get("news_count"),
        "component_consensus": components.get("consensus"),
        "component_actions": components.get("actions"),
        "component_price_target": components.get("price_target"),
        "component_news": components.get("news"),
    }


def append_daily(
    sentiments: list[dict[str, Any]],
    run_date: date | None = None,
    panel_dir: Path | None = None,
) -> Path | None:
    """Write one run's sentiment rows as a dated Parquet partition.

    Returns the path written, or None if there was nothing to write. Re-running the
    same day overwrites that day's partition only, which keeps the job idempotent
    without ever touching another date.

    Raises on write failure so the caller can decide; the daily job treats it as
    non-fatal, since losing a panel row must never cost us the run itself.
    """
    if not sentiments:
        return None

    import pandas as pd  # local: keeps module import cheap for callers that never write

    run_date = run_date or datetime.now(timezone.utc).date()
    as_of = datetime.now(timezone.utc).isoformat()
    root = Path(panel_dir) if panel_dir is not None else PANEL_DIR

    frame = pd.DataFrame([_row(s, run_date, as_of) for s in sentiments], columns=COLUMNS)
    frame = frame.dropna(subset=["ticker"]).drop_duplicates(subset=["ticker"], keep="last")
    if frame.empty:
        return None

    partition = root / f"date={run_date.isoformat()}"
    partition.mkdir(parents=True, exist_ok=True)
    out = partition / "part-0.parquet"
    frame.to_parquet(out, index=False)
    return out


def read_panel(panel_dir: Path | None = None):
    """The whole accumulated panel as one DataFrame. Empty frame when nothing exists
    yet, so callers do not have to special-case a cold start."""
    import pandas as pd

    root = Path(panel_dir) if panel_dir is not None else PANEL_DIR
    files = sorted(root.glob("date=*/*.parquet"))
    if not files:
        return pd.DataFrame(columns=COLUMNS)
    return pd.concat((pd.read_parquet(f) for f in files), ignore_index=True)


def panel_summary(panel_dir: Path | None = None) -> dict[str, Any]:
    """Cheap health read for /health: how long the clock has actually been running.
    Counts partitions rather than reading them, so it stays O(days) not O(rows)."""
    root = Path(panel_dir) if panel_dir is not None else PANEL_DIR
    days = sorted(p.name.removeprefix("date=") for p in root.glob("date=*") if p.is_dir())
    return {
        "dir": str(root),
        "days": len(days),
        "first": days[0] if days else None,
        "last": days[-1] if days else None,
    }
