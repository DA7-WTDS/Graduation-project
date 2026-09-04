# Tests for core/news_store.py — the permanent headline archive.
# Standalone, no pytest, no network:  python test_news_store.py
#
# The properties that matter are archival ones: nothing already stored may be
# lost or altered by a later write, and nothing may be placed at a time it did
# not happen (which would make it visible to a replay date that could not have
# seen it).

import shutil
import tempfile
from datetime import date, datetime, timezone
from pathlib import Path

from core import news_store as ns


def tmp():
    return Path(tempfile.mkdtemp(prefix="qw_news_"))


def row(ticker="AAPL", headline="Apple beats", when="2026-08-14T12:00:00+00:00", vendor_id=None, seen=None):
    return ns.normalize(ticker=ticker, headline=headline, published_at=when,
                        source="wire", url="u", vendor_id=vendor_id, first_seen_at=seen)


# ---- partitioning ----------------------------------------------------------

def test_partitions_by_publication_date():
    d = tmp()
    try:
        ns.append([row(when="2026-08-14T12:00:00+00:00", headline="a"),
                   row(when="2026-08-15T09:00:00+00:00", headline="b")], d)
        assert sorted(p.name for p in d.glob("date=*")) == ["date=2026-08-14", "date=2026-08-15"]
    finally:
        shutil.rmtree(d)


def test_partition_follows_publication_not_ingestion():
    """A story published at 23:50 is fetched the next morning. It must land in the
    day it was published, or point-in-time slicing puts it a day late."""
    d = tmp()
    try:
        ns.append([row(when="2026-08-14T23:50:00+00:00", seen="2026-08-15T01:00:00+00:00")], d)
        assert [p.name for p in d.glob("date=*")] == ["date=2026-08-14"]
    finally:
        shutil.rmtree(d)


# ---- the archival guarantees ------------------------------------------------

def test_appending_the_same_batch_twice_is_a_no_op():
    d = tmp()
    try:
        batch = [row(headline="a"), row(headline="b")]
        first = ns.append(batch, d)
        second = ns.append(batch, d)
        assert first["written"] == 2
        assert second["written"] == 0 and second["duplicates"] == 2
        assert len(ns.read(store_dir=d)) == 2
    finally:
        shutil.rmtree(d)


def test_appending_to_an_existing_day_keeps_what_was_there():
    """The nightly run adds to yesterday's partition. Truncating instead of merging
    would silently destroy the archive one day at a time."""
    d = tmp()
    try:
        ns.append([row(headline="old one"), row(headline="old two")], d)
        ns.append([row(headline="new one")], d)
        got = {h for h in ns.read(store_dir=d)["headline"]}
        assert got == {"old one", "old two", "new one"}, got
    finally:
        shutil.rmtree(d)


def test_a_later_backfill_cannot_rewrite_when_we_first_knew():
    """first_seen_at is a fact about our own history. A backfill run months later
    must not restamp it, or the live-vs-replay divergence measurement (§ C.4)
    silently loses its baseline."""
    d = tmp()
    try:
        ns.append([row(headline="a", seen="2026-08-14T13:00:00+00:00")], d)
        ns.append([row(headline="a", seen="2027-01-01T00:00:00+00:00")], d)
        assert ns.read(store_dir=d)["first_seen_at"].iloc[0].startswith("2026-08-14")
    finally:
        shutil.rmtree(d)


def test_undated_headlines_are_dropped_not_stamped_with_now():
    """Guessing a timestamp would put the row in the wrong partition and expose it
    to a replay date that could not have seen it. A gap beats silent leakage."""
    assert ns.normalize("AAPL", "no timestamp", None) is None
    assert ns.normalize("AAPL", "unparseable", "not a date") is None
    assert ns.normalize("AAPL", "   ", "2026-08-14T12:00:00+00:00") is None


# ---- identity ---------------------------------------------------------------

def test_the_same_story_under_two_tickers_is_two_rows():
    """A piece about a supplier is genuinely news for both names, and each needs
    its own row or one ticker's window loses it."""
    d = tmp()
    try:
        ns.append([row(ticker="AAPL", headline="Chip supplier cuts guidance", vendor_id=99),
                   row(ticker="TSM", headline="Chip supplier cuts guidance", vendor_id=99)], d)
        assert len(ns.read(store_dir=d)) == 2
    finally:
        shutil.rmtree(d)


def test_dedup_falls_back_to_content_when_the_vendor_gives_no_id():
    d = tmp()
    try:
        ns.append([row(headline="same story"), row(headline="same story")], d)
        assert len(ns.read(store_dir=d)) == 1
    finally:
        shutil.rmtree(d)


def test_timestamp_formats_normalize_to_one_representation():
    epoch = ns.normalize("AAPL", "h", 1786000000)
    iso = ns.normalize("AAPL", "h", "2026-08-14T12:00:00Z")
    naive = ns.normalize("AAPL", "h", datetime(2026, 8, 14, 12, 0))
    for r in (epoch, iso, naive):
        assert r is not None and r["published_at"].endswith("+00:00"), r
    assert iso["published_at"] == naive["published_at"]


# ---- reading ----------------------------------------------------------------

def test_read_filters_by_ticker_and_date():
    d = tmp()
    try:
        ns.append([
            row(ticker="AAPL", headline="a", when="2026-08-10T12:00:00+00:00"),
            row(ticker="MSFT", headline="b", when="2026-08-14T12:00:00+00:00"),
            row(ticker="AAPL", headline="c", when="2026-08-20T12:00:00+00:00"),
        ], d)
        assert len(ns.read(tickers=["AAPL"], store_dir=d)) == 2
        assert len(ns.read(start=date(2026, 8, 12), store_dir=d)) == 2
        assert len(ns.read(tickers=["AAPL"], start=date(2026, 8, 12), store_dir=d)) == 1
    finally:
        shutil.rmtree(d)


def test_cold_start_reads_empty_rather_than_failing():
    d = tmp()
    try:
        assert ns.read(store_dir=d).empty
        assert ns.summary(store_dir=d)["days"] == 0
    finally:
        shutil.rmtree(d)


def test_summary_reports_coverage():
    d = tmp()
    try:
        for day in ("2026-08-01", "2026-08-05", "2026-08-09"):
            ns.append([row(headline=day, when=f"{day}T12:00:00+00:00")], d)
        s = ns.summary(store_dir=d)
        assert (s["days"], s["first"], s["last"]) == (3, "2026-08-01", "2026-08-09")
    finally:
        shutil.rmtree(d)


def test_results_are_chronological():
    d = tmp()
    try:
        ns.append([row(headline="c", when="2026-08-20T12:00:00+00:00"),
                   row(headline="a", when="2026-08-10T12:00:00+00:00"),
                   row(headline="b", when="2026-08-14T12:00:00+00:00")], d)
        assert list(ns.read(store_dir=d)["headline"]) == ["a", "b", "c"]
    finally:
        shutil.rmtree(d)


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
