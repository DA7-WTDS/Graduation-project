# Tests for replay/build_corpus.py — the adaptive pagination that MVP_PLAN § C.0
# says is mandatory, because /company-news truncates at ~250 items and says nothing.
# Standalone, no pytest, no network:  python test_replay_corpus.py

from datetime import date, datetime, timedelta, timezone

from replay import build_corpus as bc


def _item(day: date, n: int):
    ts = int(datetime(day.year, day.month, day.day, 12, tzinfo=timezone.utc).timestamp()) + n
    return {"id": f"{day.isoformat()}-{n}", "datetime": ts,
            "headline": f"{day.isoformat()} story {n}", "source": "wire", "url": "u"}


class FakeApi:
    """Stands in for /company-news, reproducing the two behaviours that matter:
    a hard per-response cap, and returning the NEWEST items at or before `to`."""

    def __init__(self, per_day: dict[date, int], cap: int = bc.PAGE_CAP):
        self.per_day = per_day
        self.cap = cap
        self.calls: list[tuple[date, date]] = []

    def __call__(self, ticker, frm, to, key):
        self.calls.append((frm, to))
        items = []
        day = frm
        while day <= to:
            items += [_item(day, n) for n in range(self.per_day.get(day, 0))]
            day += timedelta(days=1)
        items.sort(key=lambda x: x["datetime"], reverse=True)   # newest first
        return items[: self.cap]                                 # newest tail only


def run(per_day, start, end, cap=bc.PAGE_CAP):
    fake = FakeApi(per_day, cap)
    original = bc._news_slice
    bc._news_slice = fake
    try:
        items, calls = bc.fetch_news("TEST", start, end, "key")
    finally:
        bc._news_slice = original
    return items, calls, fake


def test_recovers_everything_from_a_quiet_window():
    start, end = date(2026, 1, 1), date(2026, 1, 10)
    per_day = {start + timedelta(days=i): 3 for i in range(10)}
    items, _, _ = run(per_day, start, end)
    assert len(items) == 30, len(items)


def test_a_capped_slice_is_split_rather_than_truncated():
    """The failure this whole design exists to prevent: one call over a busy window
    returns the cap and drops the rest with no error."""
    start, end = date(2026, 1, 1), date(2026, 1, 14)
    per_day = {start + timedelta(days=i): 100 for i in range(14)}   # 1400 total
    items, calls, fake = run(per_day, start, end)

    assert len(items) == 1400, f"lost {1400 - len(items)} headlines"
    assert calls > 1, "a capped window must be subdivided"
    # A single naive call would have returned only the cap.
    assert bc.PAGE_CAP < 1400


def test_deduplicates_across_overlapping_slices():
    start, end = date(2026, 1, 1), date(2026, 1, 5)
    per_day = {start + timedelta(days=i): 4 for i in range(5)}
    items, _, _ = run(per_day, start, end)
    keys = [(i["headline"], i["published_at"]) for i in items]
    assert len(keys) == len(set(keys)), "duplicate rows survived"


def test_stops_subdividing_at_a_single_day():
    """One day that still hits the cap is genuinely more than the endpoint gives.
    Recursing past that would spin forever and burn the rate limit."""
    day = date(2026, 1, 1)
    items, calls, _ = run({day: 5000}, day, day)
    assert calls < 10, f"runaway recursion: {calls} calls for one day"
    assert len(items) == bc.PAGE_CAP


def test_a_failed_call_does_not_lose_the_rest_of_the_window():
    start, end = date(2026, 1, 1), date(2026, 1, 28)
    per_day = {start + timedelta(days=i): 5 for i in range(28)}
    fake = FakeApi(per_day)

    calls = {"n": 0}

    def flaky(ticker, frm, to, key):
        calls["n"] += 1
        if calls["n"] == 2:
            return None            # one transient failure
        return fake(ticker, frm, to, key)

    original = bc._news_slice
    bc._news_slice = flaky
    try:
        items, _ = bc.fetch_news("TEST", start, end, "key")
    finally:
        bc._news_slice = original

    # The failed slice's days are missing, but every other slice survived.
    assert 0 < len(items) < 140


def test_empty_window_is_not_an_error():
    start, end = date(2026, 1, 1), date(2026, 1, 10)
    items, calls, _ = run({}, start, end)
    assert items == [] and calls > 0


def test_results_are_chronological():
    start, end = date(2026, 1, 1), date(2026, 1, 20)
    per_day = {start + timedelta(days=i): 6 for i in range(20)}
    items, _, _ = run(per_day, start, end)
    stamps = [i["published_at"] for i in items]
    assert stamps == sorted(stamps)


def test_covers_the_whole_requested_window():
    """Slices must tile the range with no gap between them — an off-by-one in the
    cursor walk would drop a day per slice and nothing would complain."""
    start, end = date(2026, 1, 1), date(2026, 3, 1)
    per_day = {start + timedelta(days=i): 2 for i in range((end - start).days + 1)}
    items, _, _ = run(per_day, start, end)
    days = {i["published_at"][:10] for i in items}
    expected = {(start + timedelta(days=i)).isoformat() for i in range((end - start).days + 1)}
    assert days == expected, f"missing days: {sorted(expected - days)[:5]}"


def test_never_requests_outside_the_window():
    start, end = date(2026, 2, 1), date(2026, 2, 20)
    per_day = {start + timedelta(days=i): 1 for i in range(20)}
    _, _, fake = run(per_day, start, end)
    for frm, to in fake.calls:
        assert frm >= start and to <= end, f"slice {frm}->{to} escaped the window"


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
