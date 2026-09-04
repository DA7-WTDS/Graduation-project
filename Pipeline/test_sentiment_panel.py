# Tests for core/sentiment_panel.py (MVP_PLAN § B clock 1).
# Standalone, no pytest:  python test_sentiment_panel.py

import shutil
import tempfile
from datetime import date
from pathlib import Path

from core.sentiment_panel import COLUMNS, append_daily, panel_summary, read_panel


def sentiment(ticker="AAPL", score=0.4, news=5):
    return {
        "ticker": ticker, "sentiment_score": score, "signal": "POSITIVE",
        "analyst_rating": 4.2, "rating_label": "Buy", "ratings_count": 30,
        "recent_action": "upgrade", "recent_action_firm": "Acme", "recent_actions_count": 2,
        "days_since_latest": 3, "pt_current": 100.0, "pt_mean": 120.0, "pt_upside_pct": 20.0,
        "news_score": 0.31, "news_label": "POSITIVE", "news_count": news,
        "components": {"consensus": 0.6, "actions": 0.5, "price_target": 0.8, "news": 0.31},
    }


def tmp():
    return Path(tempfile.mkdtemp(prefix="qw_panel_"))


def test_writes_one_partition_per_date():
    d = tmp()
    try:
        append_daily([sentiment("AAPL"), sentiment("MSFT")], date(2026, 9, 1), d)
        append_daily([sentiment("AAPL")], date(2026, 9, 2), d)
        assert sorted(p.name for p in d.glob("date=*")) == ["date=2026-09-01", "date=2026-09-02"]
        assert len(read_panel(d)) == 3
    finally:
        shutil.rmtree(d)


def test_rerunning_a_day_replaces_only_that_day():
    """The daily job is retried on failure; a retry must not double-count, and must
    not be able to touch a date it is not writing."""
    d = tmp()
    try:
        append_daily([sentiment("AAPL"), sentiment("MSFT")], date(2026, 9, 1), d)
        append_daily([sentiment("NVDA")], date(2026, 9, 2), d)
        append_daily([sentiment("AAPL", score=0.9)], date(2026, 9, 1), d)   # retry of day 1
        panel = read_panel(d)
        assert len(panel) == 2, panel
        day1 = panel[panel["date"] == "2026-09-01"]
        assert list(day1["ticker"]) == ["AAPL"] and float(day1["sentiment_score"].iloc[0]) == 0.9
        assert list(panel[panel["date"] == "2026-09-02"]["ticker"]) == ["NVDA"]
    finally:
        shutil.rmtree(d)


def test_stores_raw_components_not_just_the_composite():
    """The composite's weights are a serving decision that will change. Storing only
    the composite would make the panel unusable under any other weighting."""
    d = tmp()
    try:
        append_daily([sentiment()], date(2026, 9, 1), d)
        row = read_panel(d).iloc[0]
        assert float(row["component_consensus"]) == 0.6
        assert float(row["component_news"]) == 0.31
        assert float(row["component_price_target"]) == 0.8
    finally:
        shutil.rmtree(d)


def test_missing_components_survive_as_nulls():
    """Renormalization means a record can legitimately lack a component. That is a
    fact about the day, not a reason to drop the row."""
    d = tmp()
    try:
        s = sentiment()
        s["components"] = {"news": 0.2}
        s["analyst_rating"] = None
        append_daily([s], date(2026, 9, 1), d)
        row = read_panel(d).iloc[0]
        assert float(row["component_news"]) == 0.2
        assert row["component_consensus"] is None or row["component_consensus"] != row["component_consensus"]
    finally:
        shutil.rmtree(d)


def test_schema_is_stable():
    d = tmp()
    try:
        append_daily([sentiment()], date(2026, 9, 1), d)
        assert list(read_panel(d).columns) == COLUMNS
    finally:
        shutil.rmtree(d)


def test_empty_input_writes_nothing():
    d = tmp()
    try:
        assert append_daily([], date(2026, 9, 1), d) is None
        assert list(d.glob("date=*")) == []
    finally:
        shutil.rmtree(d)


def test_duplicate_tickers_collapse():
    d = tmp()
    try:
        append_daily([sentiment("AAPL", score=0.1), sentiment("AAPL", score=0.7)], date(2026, 9, 1), d)
        panel = read_panel(d)
        assert len(panel) == 1 and float(panel["sentiment_score"].iloc[0]) == 0.7
    finally:
        shutil.rmtree(d)


def test_summary_reports_the_clock():
    d = tmp()
    try:
        assert panel_summary(d) == {"dir": str(d), "days": 0, "first": None, "last": None}
        for day in (1, 2, 5):
            append_daily([sentiment()], date(2026, 9, day), d)
        s = panel_summary(d)
        assert (s["days"], s["first"], s["last"]) == (3, "2026-09-01", "2026-09-05")
    finally:
        shutil.rmtree(d)


def test_cold_start_reads_empty_not_error():
    d = tmp()
    try:
        assert read_panel(d).empty
    finally:
        shutil.rmtree(d)


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
