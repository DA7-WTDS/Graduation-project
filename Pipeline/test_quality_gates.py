# Tests for core/quality_gates.py (IMPLEMENTATION_PLAN § 6.2).
# Standalone, no pytest dependency:  python test_quality_gates.py

from datetime import date

from core.quality_gates import run_quality_gates, trading_days_elapsed

US_CAL = {"calendar": {"trading_days": ["Mon", "Tue", "Wed", "Thu", "Fri"]}}
EGX_CAL = {"calendar": {"trading_days": ["Sun", "Mon", "Tue", "Wed", "Thu"]}}


def record(ticker="AAPL", change_pct=3.0, rsi=50.0, sma=1.0, analyst=2.0, news=None):
    return {
        "ticker": ticker, "change_pct": change_pct,
        "rsi_14": rsi, "pct_vs_sma50": sma,
        "analyst_rating": analyst, "news_score": news,
    }


def records(n, **kw):
    return [record(ticker=f"T{i}", **kw) for i in range(n)]


def check(report, name):
    return next(c for c in report.checks if c.name == name)


def test_clean_run_passes():
    r = run_quality_gates(records(80), 100, date(2026, 7, 16), US_CAL, today=date(2026, 7, 17))
    assert r.passed, r.failures
    assert {c.name for c in r.checks} == {
        "coverage", "staleness", "sanity.change_pct", "sanity.features", "sanity.sentiment"}


def test_low_coverage_fails():
    r = run_quality_gates(records(40), 100, date(2026, 7, 16), US_CAL, today=date(2026, 7, 17))
    assert not check(r, "coverage").passed
    assert "40/100" in r.failures[0]


def test_coverage_threshold_from_config():
    cfg = {**US_CAL, "quality": {"min_coverage": 0.30}}
    r = run_quality_gates(records(40), 100, date(2026, 7, 16), cfg, today=date(2026, 7, 17))
    assert check(r, "coverage").passed


def test_staleness_weekend_is_not_stale():
    # Friday close checked on Sunday: 0 trading days elapsed on a Mon-Fri calendar.
    assert trading_days_elapsed(date(2026, 7, 10), date(2026, 7, 12), {0, 1, 2, 3, 4}) == 0
    # Friday close checked on Monday: 1 (the pre-close morning run) — allowed by default.
    assert trading_days_elapsed(date(2026, 7, 10), date(2026, 7, 13), {0, 1, 2, 3, 4}) == 1
    # Friday close checked on Wednesday: 3 — stale.
    r = run_quality_gates(records(80), 100, date(2026, 7, 10), US_CAL, today=date(2026, 7, 15))
    assert not check(r, "staleness").passed


def test_staleness_uses_market_calendar():
    # Thursday close checked on Saturday: Fri is a trading day for US (stale-ish at 1)
    # but on the EGX Sun-Thu calendar Fri+Sat are weekend → 0 elapsed.
    us = run_quality_gates(records(80), 100, date(2026, 7, 16), US_CAL, today=date(2026, 7, 18))
    egx = run_quality_gates(records(80), 100, date(2026, 7, 16), EGX_CAL, today=date(2026, 7, 18))
    assert check(us, "staleness").detail.startswith("newest close 2026-07-16 is 1")
    assert check(egx, "staleness").detail.startswith("newest close 2026-07-16 is 0")


def test_missing_price_dates_fail_staleness():
    r = run_quality_gates(records(80), 100, None, US_CAL, today=date(2026, 7, 17))
    assert not check(r, "staleness").passed


def test_change_pct_outlier_fails_and_names_ticker():
    recs = records(80)
    recs[3]["change_pct"] = -31.0
    r = run_quality_gates(recs, 100, date(2026, 7, 16), US_CAL, today=date(2026, 7, 17))
    c = check(r, "sanity.change_pct")
    assert not c.passed and "T3" in c.detail


def test_feature_coverage_fails_when_blocks_null():
    recs = records(80, rsi=None)
    r = run_quality_gates(recs, 100, date(2026, 7, 16), US_CAL, today=date(2026, 7, 17))
    assert not check(r, "sanity.features").passed


def test_sentiment_gate_respects_config():
    recs = records(80, analyst=None, news=None)
    strict = run_quality_gates(recs, 100, date(2026, 7, 16), US_CAL, today=date(2026, 7, 17))
    assert not check(strict, "sanity.sentiment").passed

    lax = {**US_CAL, "quality": {"sentiment_required": False}}
    r = run_quality_gates(recs, 100, date(2026, 7, 16), lax, today=date(2026, 7, 17))
    assert "sanity.sentiment" not in {c.name for c in r.checks}


def test_empty_run_fails_everything_reasonable():
    r = run_quality_gates([], 100, None, US_CAL, today=date(2026, 7, 17))
    assert not r.passed
    assert not check(r, "coverage").passed
    assert not check(r, "sanity.features").passed


if __name__ == "__main__":
    failures = 0
    for name, fn in sorted({k: v for k, v in globals().items() if k.startswith("test_")}.items()):
        try:
            fn()
            print(f"PASS {name}")
        except AssertionError as e:
            failures += 1
            print(f"FAIL {name}: {e}")
    raise SystemExit(failures)
