# QuantWise — analyst upgrade/downgrade scoring, as one shared implementation.
#
# Extracted from markets/us/provider.py for the same reason as the sentiment
# composite: point-in-time replay must score through the identical window and
# weighting as live (MVP_PLAN § C.2 rule 3). Live gets its rows from a yfinance
# frame, replay gets them from the corpus ledger sliced at the as-of cutoff — the
# arithmetic between those two must not be two implementations.
#
# Pure: no network, no vendor types, no clock. `as_of` is always passed in, which
# is what makes a past date replayable at all.

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timedelta

# Recency window for the action score. A rating action stops counting after this.
SENTIMENT_WINDOW_DAYS = 30

# Rating vocabulary → [-1, 1]. Firms use many words for the same call, and an
# unmapped grade is treated as "no opinion about the level" rather than neutral,
# so only the direction of the action itself counts for that row.
GRADE_MAP: dict[str, float] = {
    "strong buy": 1.0, "conviction buy": 1.0, "buy": 0.6, "outperform": 0.6,
    "overweight": 0.6, "accumulate": 0.5, "add": 0.5, "positive": 0.6,
    "market outperform": 0.6, "sector outperform": 0.6, "long-term buy": 0.5,
    "hold": 0.0, "neutral": 0.0, "equal-weight": 0.0, "equalweight": 0.0,
    "market perform": 0.0, "sector perform": 0.0, "in-line": 0.0, "peer perform": 0.0,
    "reduce": -0.5, "sell": -0.6, "underperform": -0.6, "underweight": -0.6,
    "negative": -0.6, "market underperform": -0.6, "sector underperform": -0.6,
    "strong sell": -1.0,
}

ACTION_LABEL = {
    "up": "upgrade", "down": "downgrade", "init": "initiated",
    "main": "maintained", "reit": "reiterated",
}


@dataclass(frozen=True)
class ActionRow:
    """One rating action, vendor-neutral. `graded_at` is what makes it point-in-time."""
    graded_at: datetime
    action: str = ""
    to_grade: str = ""
    firm: str | None = None


@dataclass(frozen=True)
class ActionSummary:
    action_score: float | None
    latest_action: str
    latest_firm: str | None
    recent_count: int
    days_since_latest: int | None


EMPTY = ActionSummary(None, "none", None, 0, None)


def score_actions(
    rows: list[ActionRow],
    as_of: datetime,
    window_days: int = SENTIMENT_WINDOW_DAYS,
) -> ActionSummary:
    """Recency-weighted analyst-action score as of `as_of`.

    Rows dated after `as_of` are dropped, not trusted to be absent: during replay
    the caller holds the full ledger to 2026, and a single leaked row would put
    tomorrow's downgrade into today's score. Filtering here rather than relying on
    every caller to slice correctly is the point of a shared function.

    Weighting: each action counts 1.0 when it happened today, decaying linearly to
    a 0.1 floor at the window edge, so a month-old upgrade still registers faintly
    instead of vanishing between one run and the next. An action with a known
    target grade blends direction and level 50/50; an unmapped grade falls back to
    direction alone.
    """
    known = [r for r in rows if r.graded_at <= as_of]
    if not known:
        return EMPTY

    known.sort(key=lambda r: r.graded_at)
    latest = known[-1]
    latest_action = ACTION_LABEL.get(latest.action.lower(), latest.action or "none")
    latest_firm = latest.firm or None
    days_since = max(0, (as_of - latest.graded_at).days)

    cutoff = as_of - timedelta(days=window_days)
    recent = [r for r in known if r.graded_at >= cutoff]
    if not recent:
        return ActionSummary(None, latest_action, latest_firm, 0, days_since)

    num = den = 0.0
    for r in recent:
        days_ago = max(0, (as_of - r.graded_at).days)
        weight = max(0.1, 1.0 - days_ago / window_days)
        action = r.action.lower()
        direction = 1.0 if action == "up" else -1.0 if action == "down" else 0.0
        grade = GRADE_MAP.get(r.to_grade.lower())
        row_score = direction if grade is None else 0.5 * direction + 0.5 * grade
        num += weight * row_score
        den += weight

    score = round(num / den, 3) if den else None
    return ActionSummary(score, latest_action, latest_firm, len(recent), days_since)
