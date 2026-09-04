# QuantWise — the news store: a permanent, append-only archive of raw headlines.
#
# Why this exists as its own thing, separate from both the replay corpus and the
# sentiment panel:
#
#   • Finnhub's retention is ROLLING, ~12 months. Every day that passes, one more
#     day of headlines becomes unfetchable at any price. The nightly run already
#     downloads headlines, scores them, and throws the text away — discarding the
#     one input in this system that cannot be re-acquired later.
#   • The replay corpus is a one-shot SNAPSHOT and `--refresh` overwrites it. Good
#     enough to drive a backfill, not a place to keep anything.
#   • The sentiment panel stores derived SCORES, not text. A score is one model's
#     reading of a headline; swap FinBERT for the Gemini extractor (§ 1.6) and
#     every stored score is stale while the text would still be perfectly good.
#
# So: text here, scores elsewhere. Raw vendor facts only, never a derivation of
# them, because derivations can always be recomputed and text cannot be refetched.
#
# Layout: hive-partitioned by PUBLICATION date, which is the key every reader
# slices on (point-in-time replay asks "what was published on or before t").
#
#   news_store/date=2026-08-14/part-0.parquet
#
# Appending to an existing partition MERGES and deduplicates; it never truncates.
# That matters because a nightly run legitimately adds to the previous day's
# partition (a story published at 23:50 is fetched the next morning), so
# partitions cannot be write-once. The tests pin the merge: appending the same
# batch twice is a no-op, and appending new rows preserves the old ones.

from __future__ import annotations

import hashlib
import logging
import os
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any, Iterable

log = logging.getLogger(__name__)

BASE_DIR = Path(__file__).parent.parent

# Overridable so the container can point this at a mounted volume. An archive
# living in an ephemeral container filesystem is worse than no archive: it looks
# like history is accumulating right up until the moment you need it.
STORE_DIR = Path(os.getenv("NEWS_STORE_DIR") or (BASE_DIR / "training" / "data" / "news_store"))

COLUMNS = [
    "uid",            # stable dedup key
    "ticker",
    "published_at",   # vendor timestamp — the point-in-time key
    "headline",
    "source",
    "url",
    "first_seen_at",  # when WE learned of it
]


def _uid(ticker: str, published_at: str, headline: str, vendor_id: Any = None) -> str:
    """Stable dedup key.

    Prefers the vendor's own id, but still scopes it by ticker: the same article
    is returned under every symbol it mentions, and those are genuinely different
    rows (a Reuters piece about a supplier is news for both names).
    """
    if vendor_id not in (None, "", 0):
        return f"{ticker}:{vendor_id}"
    digest = hashlib.sha1(f"{ticker}|{published_at}|{headline.strip().lower()}".encode()).hexdigest()
    return f"{ticker}:h{digest[:16]}"


def normalize(
    ticker: str,
    headline: str,
    published_at: datetime | str | int | None,
    source: str | None = None,
    url: str | None = None,
    vendor_id: Any = None,
    first_seen_at: str | None = None,
) -> dict[str, Any] | None:
    """One vendor item as a store row, or None if it cannot be placed in time.

    A headline with no usable timestamp is DROPPED rather than stamped with 'now'.
    Guessing would put it in the wrong partition and make it visible to a replay
    date that could not have seen it — silent leakage, which is worse than a gap.
    """
    headline = (headline or "").strip()
    if not headline:
        return None

    ts: datetime | None = None
    if isinstance(published_at, (int, float)) and published_at > 0:
        ts = datetime.fromtimestamp(int(published_at), tz=timezone.utc)
    elif isinstance(published_at, datetime):
        ts = published_at if published_at.tzinfo else published_at.replace(tzinfo=timezone.utc)
    elif isinstance(published_at, str) and published_at:
        try:
            parsed = datetime.fromisoformat(published_at.replace("Z", "+00:00"))
            ts = parsed if parsed.tzinfo else parsed.replace(tzinfo=timezone.utc)
        except ValueError:
            ts = None
    if ts is None:
        return None

    iso = ts.isoformat()
    return {
        "uid": _uid(ticker, iso, headline, vendor_id),
        "ticker": ticker,
        "published_at": iso,
        "headline": headline,
        "source": (source or None),
        "url": (url or None),
        "first_seen_at": first_seen_at or datetime.now(timezone.utc).isoformat(),
    }


def append(rows: Iterable[dict[str, Any]], store_dir: Path | None = None) -> dict[str, int]:
    """Merge rows into the store. Returns {received, written, duplicates, undated}.

    Idempotent: re-running a day's ingest adds nothing. Existing rows win over
    incoming ones on a uid collision, so the ORIGINAL first_seen_at survives — the
    moment we first knew something is a fact about our own history and must not be
    rewritten by a later backfill.
    """
    import pandas as pd

    rows = [r for r in rows if r]
    stats = {"received": len(rows), "written": 0, "duplicates": 0, "undated": 0}
    if not rows:
        return stats

    root = Path(store_dir) if store_dir is not None else STORE_DIR
    frame = pd.DataFrame(rows, columns=COLUMNS)
    stats["undated"] = int(frame["published_at"].isna().sum())
    frame = frame.dropna(subset=["published_at", "uid"])
    if frame.empty:
        return stats

    frame["_day"] = frame["published_at"].str.slice(0, 10)

    for day, chunk in frame.groupby("_day"):
        partition = root / f"date={day}"
        partition.mkdir(parents=True, exist_ok=True)
        path = partition / "part-0.parquet"

        incoming = chunk.drop(columns=["_day"]).drop_duplicates(subset=["uid"], keep="first")
        if path.exists():
            existing = pd.read_parquet(path)
            before = len(existing)
            merged = pd.concat([existing, incoming], ignore_index=True)
            # keep="first" => existing rows win, preserving their first_seen_at
            merged = merged.drop_duplicates(subset=["uid"], keep="first")
            added = len(merged) - before
            stats["duplicates"] += len(incoming) - added
        else:
            merged = incoming
            added = len(merged)

        if added or not path.exists():
            merged.sort_values("published_at").to_parquet(path, index=False)
        stats["written"] += added

    return stats


def read(
    tickers: list[str] | None = None,
    start: date | None = None,
    end: date | None = None,
    store_dir: Path | None = None,
):
    """Stored headlines, optionally filtered. Empty frame when nothing matches, so
    a cold start needs no special-casing by callers."""
    import pandas as pd

    root = Path(store_dir) if store_dir is not None else STORE_DIR
    if not root.exists():
        return pd.DataFrame(columns=COLUMNS)

    wanted = set(tickers) if tickers else None
    frames = []
    for partition in sorted(root.glob("date=*")):
        day = partition.name.removeprefix("date=")
        try:
            d = date.fromisoformat(day)
        except ValueError:
            continue
        if (start and d < start) or (end and d > end):
            continue
        for path in sorted(partition.glob("*.parquet")):
            chunk = pd.read_parquet(path)
            if wanted is not None:
                chunk = chunk[chunk["ticker"].isin(wanted)]
            if not chunk.empty:
                frames.append(chunk)

    if not frames:
        return pd.DataFrame(columns=COLUMNS)
    return pd.concat(frames, ignore_index=True).sort_values("published_at").reset_index(drop=True)


def summary(store_dir: Path | None = None) -> dict[str, Any]:
    """Coverage at a glance, for /health. Counts partitions rather than reading
    them, so it stays O(days) instead of O(headlines)."""
    root = Path(store_dir) if store_dir is not None else STORE_DIR
    days = sorted(p.name.removeprefix("date=") for p in root.glob("date=*") if p.is_dir()) if root.exists() else []
    return {
        "dir": str(root),
        "days": len(days),
        "first": days[0] if days else None,
        "last": days[-1] if days else None,
    }
