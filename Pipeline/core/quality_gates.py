# Data-quality gates (IMPLEMENTATION_PLAN § 6.2).
#
# Pure functions, executed between scoring and ingest: the pipeline refuses to
# hand a bad run to the backend as publishable. Every threshold lives in the
# market config (markets/<market>/config.yaml, `quality:` section) so EGX can
# tune its own limits without touching code.
#
# A failed gate does NOT drop the run — the run still crosses to the backend,
# flagged `quarantined`, where it is persisted for audit but invisible to the
# optimizer and to users (DailyRun.Status kill switch on the .NET side).

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date, datetime, timedelta
from typing import Any

# ISO weekday name -> Python weekday() int (Mon=0).
_WEEKDAYS = {"Mon": 0, "Tue": 1, "Wed": 2, "Thu": 3, "Fri": 4, "Sat": 5, "Sun": 6}

_DEFAULTS = {
    "min_coverage": 0.60,             # scored tickers >= 60% of universe
    "max_staleness_trading_days": 1,  # newest close may lag "today" by <= 1 trading day
    "max_abs_change_pct": 25.0,       # no 30d prediction beyond +/-25% (fat-finger / split artifact)
    "min_feature_coverage": 0.80,     # rsi_14 & pct_vs_sma50 non-null on >= 80% of records
    "sentiment_required": True,       # at least one record carries analyst/news data
}


@dataclass
class GateCheck:
    name: str
    passed: bool
    detail: str


@dataclass
class GateReport:
    checks: list[GateCheck] = field(default_factory=list)

    @property
    def passed(self) -> bool:
        return all(c.passed for c in self.checks)

    @property
    def failures(self) -> list[str]:
        return [f"{c.name}: {c.detail}" for c in self.checks if not c.passed]


def _thresholds(config: dict[str, Any]) -> dict[str, Any]:
    merged = dict(_DEFAULTS)
    merged.update(config.get("quality") or {})
    return merged


def _trading_weekdays(config: dict[str, Any]) -> set[int]:
    names = (config.get("calendar") or {}).get("trading_days") or ["Mon", "Tue", "Wed", "Thu", "Fri"]
    return {_WEEKDAYS[d] for d in names if d in _WEEKDAYS}


def trading_days_elapsed(last_price_date: date, today: date, trading_weekdays: set[int]) -> int:
    """Number of trading days in (last_price_date, today] — how stale the data is.

    0 = we have today's close (or today is a non-trading day and we have the
    most recent one); 1 = one trading session missing, e.g. an early-morning
    run before today's close exists. Capped at 30 to keep the loop bounded.
    """
    elapsed = 0
    d = last_price_date
    for _ in range(30):
        d = d + timedelta(days=1)
        if d > today:
            break
        if d.weekday() in trading_weekdays:
            elapsed += 1
    return elapsed


def run_quality_gates(
    records: list[dict[str, Any]],
    universe_size: int,
    last_price_date: date | None,
    config: dict[str, Any],
    today: date | None = None,
) -> GateReport:
    """All § 6.2 checks against one scored run. `records` are ScoreRecord dicts."""
    t = _thresholds(config)
    today = today or datetime.utcnow().date()
    report = GateReport()

    # 1. Coverage: enough of the universe actually scored.
    coverage = (len(records) / universe_size) if universe_size > 0 else 0.0
    report.checks.append(GateCheck(
        name="coverage",
        passed=coverage >= float(t["min_coverage"]),
        detail=f"{len(records)}/{universe_size} tickers scored ({coverage:.0%}, min {float(t['min_coverage']):.0%})",
    ))

    # 2. Staleness: newest close matches the market calendar's expected trading day.
    max_lag = int(t["max_staleness_trading_days"])
    if last_price_date is None:
        report.checks.append(GateCheck("staleness", False, "no price dates available"))
    else:
        lag = trading_days_elapsed(last_price_date, today, _trading_weekdays(config))
        report.checks.append(GateCheck(
            name="staleness",
            passed=lag <= max_lag,
            detail=f"newest close {last_price_date.isoformat()} is {lag} trading day(s) old (max {max_lag})",
        ))

    # 3a. Sanity — prediction magnitude: a |change_pct| beyond the cap without a
    # corporate-action feed to explain it is treated as a data artifact.
    cap = float(t["max_abs_change_pct"])
    outliers = [r["ticker"] for r in records if abs(float(r.get("change_pct") or 0.0)) > cap]
    report.checks.append(GateCheck(
        name="sanity.change_pct",
        passed=not outliers,
        detail="all predictions within cap" if not outliers
               else f"|change_pct| > {cap}% for: {', '.join(sorted(outliers)[:10])}",
    ))

    # 3b. Sanity — feature blocks non-null: tactical inputs present on most records.
    if records:
        with_features = sum(
            1 for r in records
            if r.get("rsi_14") is not None and r.get("pct_vs_sma50") is not None
        )
        feature_cov = with_features / len(records)
    else:
        feature_cov = 0.0
    report.checks.append(GateCheck(
        name="sanity.features",
        passed=feature_cov >= float(t["min_feature_coverage"]),
        detail=f"rsi_14+pct_vs_sma50 present on {feature_cov:.0%} of records (min {float(t['min_feature_coverage']):.0%})",
    ))

    # 3c. Sanity — sentiment feed non-empty (if this market requires it).
    if bool(t["sentiment_required"]):
        with_sentiment = sum(
            1 for r in records
            if r.get("analyst_rating") is not None or r.get("news_score") is not None
        )
        report.checks.append(GateCheck(
            name="sanity.sentiment",
            passed=with_sentiment > 0,
            detail=f"{with_sentiment} record(s) carry analyst/news data",
        ))

    return report
