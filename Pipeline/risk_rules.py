"""
QuantWise — Rule-based Risk Mitigation (Python port of risk_core.js)

Deterministic, user-INDEPENDENT risk grading. Joins each ticker's prediction
and sentiment outputs, cross-validates the quant signal against the analyst/news
signal, and attaches risk_flags, risk_level, conviction_score, and rationale.

This is the single source of truth for risk grading in the unified pipeline.
Ported 1-to-1 from Risk Node/risk_core.js — thresholds and logic are identical.
"""

# ── Tunable thresholds (mirrors risk_core.js CONFIG exactly) ──────────────────
# Low conviction is RANK-BASED, not a fixed cutoff (MVP_PLAN § A follow-up 1).
#
# Under the hybrid stack the two absolute thresholds below flagged the weakest ~10%
# of a run, entirely via the confidence leg. The trees champion broke both: measured
# over its 35,515-row OOS test slice, |change_pct| < 1.5 fires on 95.9% of records
# (relative returns are small numbers) while confidence < 0.30 fires on 0.0% (isotonic
# output is floored around 0.458). One leg flagged almost everything, the other
# nothing, so the flag stopped carrying information either way.
#
# A re-fitted pair of constants would rot again at the next champion promotion
# (retrain.py runs monthly and promotes on IC, which is scale-free — nothing keeps the
# score's SCALE stable across models). Ranking within the run is immune to that, and
# it is also the right question for a ranking model: is this name distinguishable from
# the rest of today's cross-section? The same code then serves both stacks.
# 0.10 per leg, chosen against the champion's own OOS distribution so the UNION of the
# two legs reproduces the ~10% firing rate the original thresholds were built around.
# Measured across 360 daily cross-sections of the 2024-12-31 — 2026-06-09 test slice:
# 9.5% mean (range 7.1-14.1%), against 95.9% under the old absolute cutoff.
#
# Replaying the last hybrid live run through the same code gives 18% rather than its
# original 10%. That is deliberate: the tuning target is the stack that actually
# serves, and a rollback erring toward MORE caution is the safe direction to err in.
LOW_CONVICTION_QUANTILE: float = 0.10

# Fallback cutoffs, used only when a run is too small to rank (see _low_conviction).
# These are the original hybrid-era values and are correct on an absolute-return scale.
LOW_CONVICTION_PCT: float = 1.5    # |change_pct| below this  -> low conviction
LOW_CONFIDENCE: float     = 0.30   # model reliability below this -> low conviction
EXTREME_PCT: float        = 12.0   # |change_pct| above this → possible outlier
MIN_RATINGS: int          = 5      # analyst count below this → thin coverage
MIN_NEWS: int             = 3      # news count below this → thin coverage
STALE_DAYS: int           = 60     # latest analyst action older than this → stale
COMPONENT_CONFLICT: float = 0.20   # sentiment sub-signals beyond ±this in both dirs → conflict
POS_THRESHOLD: float      = 0.15   # consistent with the sentiment service

# Guard: abort the whole run if fewer than this many records survive the merge.
# Mirrors the MIN_RECORDS guard in n8n_code_node.js (was 50).
MIN_RECORDS: int = 25


# ── Direction / sentiment agreement ───────────────────────────────────────────

def compute_agreement(direction: str, signal: str) -> str:
    """
    Returns 'CONFIRMED', 'CONTRADICT', or 'NEUTRAL'.
    Mirrors computeAgreement() in risk_core.js exactly.
    """
    if signal == "POSITIVE":
        return "CONFIRMED" if direction == "UP" else "CONTRADICT"
    if signal == "NEGATIVE":
        return "CONFIRMED" if direction == "DOWN" else "CONTRADICT"
    return "NEUTRAL"


# ── Human-readable rationale ──────────────────────────────────────────────────

def build_rationale(rec: dict, agreement: str, flags: list[str]) -> str:
    """
    Builds a short | -separated rationale string for the LLM/UI.
    Mirrors buildRationale() in risk_core.js exactly.
    """
    ch = rec.get("change_pct") or 0.0
    sign = "+" if ch >= 0 else ""
    parts: list[str] = []

    direction = rec.get("direction") or ("UP" if ch >= 0 else "DOWN")
    confidence = rec.get("confidence", "n/a")
    parts.append(f"{direction} {sign}{ch}% (conf {confidence})")

    sentiment_score = rec.get("sentiment_score", "n/a")
    signal = rec.get("signal") or "NEUTRAL"
    parts.append(f"sentiment {signal} ({sentiment_score})")
    parts.append(agreement.lower())

    rating_label = rec.get("rating_label")
    if rating_label:
        ratings_count = rec.get("ratings_count") or 0
        parts.append(f"analysts {rating_label} n={ratings_count}")

    pt_upside_pct = rec.get("pt_upside_pct")
    if pt_upside_pct is not None:
        pt_sign = "+" if pt_upside_pct >= 0 else ""
        parts.append(f"PT {pt_sign}{pt_upside_pct}%")

    if flags:
        parts.append(f"flags: {', '.join(flags)}")
    else:
        parts.append("no flags")

    return " | ".join(parts)


# ── Rank-based low-conviction selection ───────────────────────────────────────────

def _weakest(records: list[dict], value, quantile: float) -> set[str]:
    """Tickers strictly weaker than the `quantile` cutoff of the run, by `value`.

    Ranks rather than thresholds a value, so the selected share is stable no matter
    what scale the current champion scores on.

    Records TIED with the cutoff are excluded, which is why this can return fewer
    than `quantile` of the run. The alternative is worse: calibrated confidence is
    heavily tied (isotonic regression emits steps, so a quarter of a run can share
    one probability), and taking a fixed count would flag some tied names and not
    others on nothing but alphabetical order — indefensible in an audit. The flag
    means "measurably weaker than the rest of today's run"; equal is not weaker.
    """
    values = sorted(value(r) for r in records)
    k = int(len(values) * quantile)
    if k == 0:
        return set()
    cutoff = values[k]
    return {str(r.get("ticker") or "") for r in records if value(r) < cutoff}


def _low_conviction(records: list[dict]) -> set[str] | None:
    """Tickers flagged `low_conviction` for this run: the weakest tenth on model
    confidence, plus the weakest tenth on |score| (the undifferentiated middle of the
    cross-section). Union, mirroring the OR the absolute thresholds used to express.

    Returns None when the run is too small for a decile to mean anything, in which
    case each record falls back to the absolute cutoffs.
    """
    if len(records) * LOW_CONVICTION_QUANTILE < 1:
        return None
    return (_weakest(records, lambda r: float(r.get("confidence") or 0.0), LOW_CONVICTION_QUANTILE)
            | _weakest(records, lambda r: abs(float(r.get("change_pct") or 0.0)), LOW_CONVICTION_QUANTILE))


# ── Enrich a single merged record ─────────────────────────────────────────────

def enrich_record(rec: dict, low_conviction: bool | None = None) -> dict:
    """
    Takes a merged prediction+sentiment dict and attaches:
      agreement, risk_flags, risk_level, conviction_score, rationale.

    `low_conviction` is decided per RUN by apply_risk_rules (see _low_conviction).
    Passing None keeps the original per-record absolute thresholds, which is what a
    caller enriching a single record in isolation gets.
    """
    flags: list[str] = []

    ch      = float(rec.get("change_pct") or 0)
    conf    = float(rec.get("confidence") or 0)
    s_score = float(rec.get("sentiment_score") or 0)
    signal  = rec.get("signal") or "NEUTRAL"
    direction = rec.get("direction") or ("UP" if ch >= 0 else "DOWN")

    agreement = compute_agreement(direction, signal)
    if agreement == "CONFIRMED":
        flags.append("signal_confirmed")
    elif agreement == "CONTRADICT":
        flags.append("signal_contradiction")

    # Conviction / signal strength (rank-based for a full run, absolute otherwise)
    if low_conviction is None:
        low_conviction = abs(ch) < LOW_CONVICTION_PCT or conf < LOW_CONFIDENCE
    if low_conviction:
        flags.append("low_conviction")

    # Extreme move (possible model outlier)
    if abs(ch) > EXTREME_PCT:
        flags.append("extreme_move")

    # Coverage
    ratings = int(rec.get("ratings_count") or 0)
    news    = int(rec.get("news_count") or 0)
    if ratings < MIN_RATINGS or news < MIN_NEWS:
        flags.append("thin_coverage")

    # Staleness
    dsl = rec.get("days_since_latest")
    if dsl is None or float(dsl) > STALE_DAYS:
        flags.append("stale_analyst")

    # Internal conflict among sentiment sub-signals
    components = rec.get("components") or {}
    vals = [v for v in components.values() if isinstance(v, (int, float))]
    has_pos = any(v >  COMPONENT_CONFLICT for v in vals)
    has_neg = any(v < -COMPONENT_CONFLICT for v in vals)
    if has_pos and has_neg:
        flags.append("internal_conflict")

    # ── Risk level ──
    if (
        "signal_contradiction" in flags
        or "internal_conflict" in flags
        or ("extreme_move" in flags and conf < 0.5)
    ):
        risk_level = "HIGH"
    elif (
        "low_conviction" in flags
        or "thin_coverage" in flags
        or "stale_analyst" in flags
        or agreement == "NEUTRAL"
    ):
        risk_level = "MEDIUM"
    else:
        risk_level = "LOW"  # confirmed + adequate coverage + decent conviction

    # ── Conviction score (0..1) ──
    conviction = 0.5 * conf + 0.3 * min(abs(s_score), 1.0)
    if agreement == "CONFIRMED":
        conviction += 0.2
    elif agreement == "CONTRADICT":
        conviction -= 0.2
    conviction = round(max(0.0, min(1.0, conviction)), 3)

    return {
        **rec,
        "agreement":       agreement,
        "risk_flags":      flags,
        "risk_level":      risk_level,
        "conviction_score": conviction,
        "rationale":       build_rationale(rec, agreement, flags),
    }


# ── Merge predict + sentiment arrays by ticker ────────────────────────────────

def _merge_by_ticker(predictions: list[dict], sentiments: list[dict]) -> list[dict]:
    """
    Left-join sentiments onto predictions by ticker.
    Prediction fields win on key clashes (e.g. 'ticker', 'signal' if duplicated).
    Mirrors mergeByTicker() in risk_core.js exactly.
    """
    sent_by_ticker: dict[str, dict] = {}
    for s in sentiments or []:
        if s and s.get("ticker"):
            sent_by_ticker[s["ticker"]] = s

    merged = []
    for p in predictions or []:
        base = sent_by_ticker.get(p.get("ticker") or "", {})
        merged.append({**base, **p})   # prediction fields win
    return merged


# ── Top-level: merge + enrich all ─────────────────────────────────────────────

def apply_risk_rules(predictions: list[dict], sentiments: list[dict]) -> list[dict]:
    """
    Merge predictions + sentiments, enrich each record with risk metadata,
    and sort by conviction_score descending (most decisive / safest first).

    Raises ValueError if fewer than MIN_RECORDS records survive the merge —
    mirrors the MIN_RECORDS guard in n8n_code_node.js.
    """
    merged = _merge_by_ticker(predictions, sentiments)

    # Low conviction is a statement about this name RELATIVE to the rest of today's
    # run, so it is resolved once, here, where the whole cross-section is in hand.
    weak = _low_conviction(merged)
    enriched = [
        enrich_record(r, None if weak is None else str(r.get("ticker") or "") in weak)
        for r in merged
    ]
    enriched.sort(key=lambda r: r.get("conviction_score", 0.0), reverse=True)

    if len(enriched) < MIN_RECORDS:
        raise ValueError(
            f"QuantWise: only {len(enriched)} records after risk merge "
            f"(need >= {MIN_RECORDS}). Predict/Sentiment likely failed or "
            f"were rate-limited — aborting; nothing ingested."
        )

    return enriched
