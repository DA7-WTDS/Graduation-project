# Tests for replay/score_asof.py — the point-in-time guarantees (MVP_PLAN § C.2).
# Standalone, no pytest, no network:  python test_replay_scorer.py
#
# The property under test is the one that decides whether a replayed track record
# means anything: scoring date t must be a function of data available at t. The
# corpus deliberately holds everything up to today, so these tests plant future
# rows and assert they cannot reach the score.

import json
import shutil
import tempfile
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

import pandas as pd

from core.analyst_actions import ActionRow, score_actions
from replay import score_asof as sa


def make_corpus(root: Path, ticker="TEST", news=(), actions=(), consensus=()):
    for kind in ("news", "actions", "consensus"):
        (root / kind).mkdir(parents=True, exist_ok=True)

    pd.DataFrame(
        [{"ticker": ticker, "published_at": ts, "headline": h, "source": "wire", "url": "u"}
         for ts, h in news],
        columns=["ticker", "published_at", "headline", "source", "url"],
    ).to_parquet(root / "news" / f"{ticker}.parquet", index=False)

    pd.DataFrame(
        [{"ticker": ticker, "graded_at": ts, "firm": "Acme", "action": a,
          "to_grade": g, "from_grade": ""} for ts, a, g in actions],
        columns=["ticker", "graded_at", "firm", "action", "to_grade", "from_grade"],
    ).to_parquet(root / "actions" / f"{ticker}.parquet", index=False)

    pd.DataFrame(
        [{"ticker": ticker, "period": p, "strong_buy": sb, "buy": b,
          "hold": h, "sell": 0, "strong_sell": 0} for p, sb, b, h in consensus],
        columns=["ticker", "period", "strong_buy", "buy", "hold", "sell", "strong_sell"],
    ).to_parquet(root / "consensus" / f"{ticker}.parquet", index=False)

    (root / "manifest.json").write_text(json.dumps({
        "news_coverage_starts": news[0][0] if news else None,
        "per_ticker": {ticker: {"company_name": "Test Corp"}},
    }), encoding="utf-8")


def iso(d: date, hour=12):
    return datetime(d.year, d.month, d.day, hour, tzinfo=timezone.utc).isoformat()


def tmp():
    return Path(tempfile.mkdtemp(prefix="qw_replay_"))


# ---- the cutoff convention -------------------------------------------------

def test_cutoff_is_the_morning_after_not_midnight():
    """§ C.2 rule 2. The live cron fires at 01:00 UTC the next day, so a session's
    after-close news IS in scope. Midnight-on-t would silently discard it."""
    cut = sa.as_of_cutoff(date(2026, 3, 10))
    assert cut == datetime(2026, 3, 11, 1, tzinfo=timezone.utc)


def test_after_close_news_is_visible_on_the_day_it_belongs_to():
    root = tmp()
    try:
        d = date(2026, 3, 10)
        # 22:00 UTC on the session date — after the close, before the 01:00 run.
        make_corpus(root, news=[(iso(d, 22), f"Test Corp story {i}") for i in range(5)])
        c = sa.Corpus(root)
        c.load(["TEST"])
        assert len(c.headlines("TEST", sa.as_of_cutoff(d))) == 5
    finally:
        shutil.rmtree(root)


# ---- leakage ---------------------------------------------------------------

def test_future_headlines_are_invisible():
    root = tmp()
    try:
        d = date(2026, 3, 10)
        past = [(iso(d - timedelta(days=i), 9), f"Test Corp past {i}") for i in range(1, 5)]
        future = [(iso(d + timedelta(days=i), 9), f"Test Corp future {i}") for i in range(1, 6)]
        make_corpus(root, news=past + future)
        c = sa.Corpus(root)
        c.load(["TEST"])
        got = c.headlines("TEST", sa.as_of_cutoff(d))
        assert got, "past headlines should survive"
        assert not any("future" in h for h in got), got
    finally:
        shutil.rmtree(root)


def test_future_analyst_actions_are_invisible():
    """The ledger runs to today. A leaked downgrade would flip the score on a day
    nobody could have known about it."""
    now = datetime(2026, 3, 11, 1, tzinfo=timezone.utc)
    rows = [
        ActionRow(now - timedelta(days=3), "up", "buy", "Acme"),
        ActionRow(now + timedelta(days=2), "down", "sell", "Beta"),   # the future
    ]
    only_past = score_actions([rows[0]], now)
    with_future = score_actions(rows, now)
    assert with_future == only_past
    assert with_future.action_score > 0, "the known upgrade should still score positive"


def test_news_outside_the_live_window_is_dropped():
    """Live uses a 14-day window. Older news must not reach a replayed score, or
    replay scores a different set than live did."""
    root = tmp()
    try:
        d = date(2026, 3, 10)
        old = [(iso(d - timedelta(days=40), 9), f"Test Corp ancient {i}") for i in range(5)]
        recent = [(iso(d - timedelta(days=2), 9), f"Test Corp recent {i}") for i in range(4)]
        make_corpus(root, news=old + recent)
        c = sa.Corpus(root)
        c.load(["TEST"])
        got = c.headlines("TEST", sa.as_of_cutoff(d))
        assert got and not any("ancient" in h for h in got), got
    finally:
        shutil.rmtree(root)


# ---- fidelity to the live path ---------------------------------------------

def test_thin_news_drops_the_component_like_live_does():
    """Live requires NEWS_MIN_RELEVANT before it scores news at all. Replay scoring
    one stray headline would give it a news component on days live had none."""
    root = tmp()
    try:
        d = date(2026, 3, 10)
        make_corpus(root, news=[(iso(d - timedelta(days=1), 9), "Test Corp lone story")])
        c = sa.Corpus(root)
        c.load(["TEST"])
        assert c.headlines("TEST", sa.as_of_cutoff(d)) == []
    finally:
        shutil.rmtree(root)


def test_irrelevant_headlines_are_filtered_out():
    root = tmp()
    try:
        d = date(2026, 3, 10)
        noise = [(iso(d - timedelta(days=1), 9), f"Unrelated market wrap {i}") for i in range(6)]
        make_corpus(root, news=noise)
        c = sa.Corpus(root)
        c.load(["TEST"])
        # None mention the ticker or the company name, so none are relevant.
        assert c.headlines("TEST", sa.as_of_cutoff(d)) == []
    finally:
        shutil.rmtree(root)


def test_consensus_uses_the_latest_bucket_at_or_before_the_cutoff():
    root = tmp()
    try:
        make_corpus(root, news=[], consensus=[
            ("2026-01-01", 10, 5, 1),
            ("2026-02-01", 1, 1, 20),     # the bucket in force in February
            ("2026-06-01", 30, 0, 0),     # the future
        ])
        c = sa.Corpus(root)
        c.load(["TEST"])
        avg, label, n = c.consensus_at("TEST", sa.as_of_cutoff(date(2026, 2, 15)))
        assert n == 22, n
        assert avg is not None and avg < 3.5, avg   # hold-heavy, not the future's strong buy
    finally:
        shutil.rmtree(root)


def test_consensus_absent_before_any_bucket():
    root = tmp()
    try:
        make_corpus(root, news=[], consensus=[("2026-05-01", 10, 5, 1)])
        c = sa.Corpus(root)
        c.load(["TEST"])
        assert c.consensus_at("TEST", sa.as_of_cutoff(date(2025, 1, 5))) == (None, None, 0)
    finally:
        shutil.rmtree(root)


def test_price_targets_are_never_loaded():
    """§ C.2 rule 3 as a structural fact, not a convention: the corpus has no
    price-target shard and the Corpus class exposes no way to read one."""
    assert not hasattr(sa.Corpus, "price_target")
    assert not hasattr(sa.Corpus, "price_targets")
    source = Path(sa.__file__).read_text(encoding="utf-8")
    assert "pt_upside_pct\": None" in source or "\"pt_upside_pct\": None" in source


def test_missing_corpus_fails_loudly():
    root = tmp()
    try:
        raised = False
        try:
            sa.Corpus(root)
        except SystemExit as e:
            raised = "build_corpus" in str(e)
        assert raised, "a missing corpus must not silently score as no-sentiment"
    finally:
        shutil.rmtree(root)


def test_trading_days_come_from_the_price_data():
    """Sessions are whatever the exchange actually traded, so holidays need no
    hard-coded calendar."""
    days = [date(2026, 3, 9), date(2026, 3, 10), date(2026, 3, 12)]   # 11th closed
    frames = {"A": pd.DataFrame({"date": pd.to_datetime([d.isoformat() for d in days])})}
    got = sa.trading_days(frames, date(2026, 3, 1), date(2026, 3, 31))
    assert got == days


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
