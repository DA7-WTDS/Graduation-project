# QuantWise — the sentiment composite, as one shared implementation.
#
# Extracted from main.py so live scoring and point-in-time replay cannot drift
# apart (MVP_PLAN § C.2 rule 3: "identical windows and weights"). A replay whose
# scoring differs from live — even by a rounding convention — measures the replay,
# not the strategy, and the whole point of § C is to produce a track record that
# means something. Making it one function is the only way that stays true after
# someone edits a threshold in six months.
#
# Everything here is pure: no network, no model, no clock. Callers supply the
# already-gathered component values. FinBERT lives in the caller because live
# scoring holds it in process state while replay batches it over a deduped corpus.

from __future__ import annotations

import math

from typing import Any

# Component weights. Renormalized over whatever is actually present, so a missing
# block (no analyst coverage, no news in the window, price targets excluded during
# replay) reweights the rest rather than being silently scored as zero — treating
# "no signal" as "neutral signal" would drag every thin-coverage name toward 0.
WEIGHTS: dict[str, float] = {
    "consensus": 0.40,
    "actions": 0.15,
    "price_target": 0.20,
    "news": 0.25,
}

POS_THRESHOLD = 0.15
NEG_THRESHOLD = -0.15

# Analyst price-target upside is mapped onto [-1, 1] against this reference: a
# +25% consensus target counts as a maximally positive price-target signal.
PT_REF_PCT = 25.0


def consensus_score(avg_rating: float | None) -> float | None:
    """Analyst consensus (1..5, 5 = strong buy) onto [-1, 1]. 3.0 (hold) is 0."""
    return None if avg_rating is None else (avg_rating - 3.0) / 2.0


def price_target_score(pt_upside_pct: float | None) -> float | None:
    """Consensus price-target upside in percent onto [-1, 1], clamped.

    NOTE: excluded entirely during replay. Vendors only expose the CURRENT target,
    so using it at a past date would leak the future into the backtest.
    """
    if pt_upside_pct is None:
        return None
    return max(-1.0, min(1.0, pt_upside_pct / PT_REF_PCT))


def label(score: float) -> str:
    """POSITIVE / NEGATIVE / NEUTRAL from a [-1, 1] score."""
    if score > POS_THRESHOLD:
        return "POSITIVE"
    if score < NEG_THRESHOLD:
        return "NEGATIVE"
    return "NEUTRAL"


def _present(value: float | None) -> bool:
    """Whether a component actually carries a signal.

    NaN counts as absent, not as a value. Components arrive as None from live
    scoring but as NaN from any pandas/Parquet path (a missing struct field reads
    back as NaN), and letting one through poisons the weighted sum to NaN — which
    then labels as NEUTRAL, because every comparison against NaN is False. The
    failure therefore looks exactly like 'no strong opinion' rather than like a
    bug, which is the worst way for it to fail.
    """
    return value is not None and not math.isnan(float(value))


def composite(
    consensus: float | None = None,
    actions: float | None = None,
    price_target: float | None = None,
    news: float | None = None,
) -> tuple[float, str, dict[str, float]]:
    """Weighted composite over the components that are present.

    Returns (score, signal, parts) where `parts` holds only the components that
    contributed — it is stored on the record and later read by the risk rules to
    detect internal conflict, so it must reflect what was actually used.

    With nothing present the score is 0.0 / NEUTRAL: no information is not the
    same as bad news, and a ticker with no coverage must not be scored as negative.
    """
    present: dict[str, float] = {}
    for name, value in (
        ("consensus", consensus),
        ("actions", actions),
        ("price_target", price_target),
        ("news", news),
    ):
        if _present(value):
            present[name] = round(float(value), 3)

    if not present:
        return 0.0, label(0.0), {}

    weight_sum = sum(WEIGHTS[k] for k in present)
    score = round(sum(present[k] * WEIGHTS[k] for k in present) / weight_sum, 3)
    return score, label(score), present


def news_score_from_finbert(outputs: list[Any]) -> float | None:
    """Mean (P(positive) − P(negative)) over one FinBERT batch.

    Takes the raw classifier output rather than running the model, so live scoring
    (model held in process state) and replay (one batched pass over a deduplicated
    corpus) share the arithmetic while differing only in how they get there.
    """
    scores = []
    for out in outputs:
        probs = {x["label"].lower(): x["score"] for x in out}
        scores.append(probs.get("positive", 0.0) - probs.get("negative", 0.0))
    if not scores:
        return None
    return round(sum(scores) / len(scores), 3)
