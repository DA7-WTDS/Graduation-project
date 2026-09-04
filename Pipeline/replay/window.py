# QuantWise — resolving the replay window (MVP_PLAN § C.2 rule 1).
#
# Two constraints bound the window from opposite ends, and both are real:
#
#   • Lower bound — the chronological split boundary in models/registry.json.
#     Replaying a date the champion trained on measures memorization, not skill.
#     Read from the registry rather than hard-coded, so promoting a model that
#     shifts the split cannot silently invalidate the replay.
#
#   • Upper bound on history — Finnhub retains roughly 12 months of company news.
#     Dates before that are replayable, but with the news component permanently
#     absent, which is a different experiment: it measures the model plus analyst
#     data, not the daily run as it actually runs.
#
# The default window is the intersection: the news-bearing stretch of the
# out-of-sample era. Requesting more only spends throttled calls on empty slices —
# ~18 wasted calls per ticker at the current boundary — and yields dates whose
# sentiment silently differs in composition from every other date.
#
# Both bounds stay overridable. Replaying the full OOS window without news is a
# legitimate thing to want (it isolates how much the news component is worth); it
# just should not be what you get by accident.

from __future__ import annotations

import json
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

MODELS_DIR = Path(__file__).parent.parent / "models"
REGISTRY = MODELS_DIR / "registry.json"

# Measured in § C.0: /company-news returns nothing for windows fully older than
# roughly this. Not a documented vendor guarantee, so the corpus manifest records
# what actually came back and that measurement wins over this constant.
NEWS_RETENTION_DAYS = 365

# Used only if the registry is missing or unreadable; matches the champion's
# recorded test_slice_from at the time of writing.
FALLBACK_OOS_START = date(2024, 12, 31)


def oos_boundary(registry_path: Path | None = None) -> date:
    """First date the champion never trained on, from the model registry."""
    path = registry_path or REGISTRY
    try:
        entries = json.loads(path.read_text(encoding="utf-8"))
        slices = [e["test_slice_from"] for e in entries if e.get("test_slice_from")]
        if slices:
            return max(date.fromisoformat(s) for s in slices)
    except Exception:
        pass
    return FALLBACK_OOS_START


def news_horizon(today: date | None = None, retention_days: int = NEWS_RETENTION_DAYS) -> date:
    """Earliest date news is expected to still be retrievable."""
    today = today or datetime.now(timezone.utc).date()
    return today - timedelta(days=retention_days)


def default_corpus_window(
    today: date | None = None,
    registry_path: Path | None = None,
    retention_days: int = NEWS_RETENTION_DAYS,
) -> tuple[date, date]:
    """(start, end) to fetch: the news-bearing part of the out-of-sample era.

    If retention reaches further back than the split boundary, the boundary wins —
    fetching news for dates the model trained on would invite replaying them.
    """
    today = today or datetime.now(timezone.utc).date()
    return max(oos_boundary(registry_path), news_horizon(today, retention_days)), today


def resolve_replay_start(
    corpus_manifest: dict,
    explicit: date | None = None,
    registry_path: Path | None = None,
) -> tuple[date, str]:
    """Where a replay should begin, and the one-line reason why.

    Prefers what the corpus actually contains over what retention theoretically
    allows: the manifest's measured first headline is ground truth, and a vendor
    that retained 11 months instead of 12 should shorten the window rather than
    produce a stretch of newsless dates nobody notices.
    """
    boundary = oos_boundary(registry_path)

    if explicit is not None:
        if explicit < boundary:
            return explicit, (
                f"explicit --start {explicit} is BEFORE the out-of-sample boundary {boundary}; "
                "these dates are in the champion's training window and do not measure skill")
        return explicit, f"explicit --start {explicit}"

    measured = corpus_manifest.get("news_coverage_starts")
    if measured:
        first_news = date.fromisoformat(str(measured)[:10])
        start = max(first_news, boundary)
        why = (f"first headline in the corpus ({first_news})"
               if start == first_news else
               f"out-of-sample boundary {boundary}, which is later than the corpus start {first_news}")
        return start, why

    start, _ = default_corpus_window(registry_path=registry_path)
    return start, f"no measured coverage in the corpus manifest; falling back to the {NEWS_RETENTION_DAYS}-day news horizon"
