# Tests for replay/finbert_cache.py — the persistence that makes a ~5-hour
# scoring pass re-runnable. Standalone, no pytest, no model:
#   python test_finbert_cache.py

import shutil
import tempfile
from pathlib import Path

from replay.finbert_cache import FinbertCache, key


def tmp():
    return Path(tempfile.mkdtemp(prefix="qw_fb_")) / "cache.parquet"


class FakeModel:
    """Stands in for the FinBERT pipeline and counts what it was asked to score,
    which is the whole point: the cache exists to make that number small."""

    def __init__(self):
        self.seen: list[str] = []

    def __call__(self, chunk, **kw):
        self.seen.extend(chunk)
        return [[{"label": "positive", "score": 0.8}, {"label": "negative", "score": 0.1}] for _ in chunk]


def warm(cache, headlines, model=None):
    cache.model = model or FakeModel()
    cache.warm(set(headlines))
    return cache.model


# ---- persistence ------------------------------------------------------------

def test_scores_survive_a_restart():
    """The failure this exists to prevent: five hours of CPU discarded at exit."""
    p = tmp()
    try:
        a = FinbertCache(cache_path=p)
        warm(a, ["one", "two", "three"])
        assert a.score(["one"]) is not None

        reloaded = FinbertCache(cache_path=p)
        assert len(reloaded.scores) == 3
        assert reloaded.score(["one"]) == a.score(["one"])
    finally:
        shutil.rmtree(p.parent)


def test_a_second_run_scores_nothing_already_known():
    p = tmp()
    try:
        first = FinbertCache(cache_path=p)
        warm(first, ["a", "b", "c"])

        second = FinbertCache(cache_path=p)
        model = warm(second, ["a", "b", "c"])
        assert model.seen == [], f"re-scored {model.seen}"
    finally:
        shutil.rmtree(p.parent)


def test_a_widened_universe_only_scores_the_new_headlines():
    """The realistic re-run: same window, more tickers. Only the delta should cost."""
    p = tmp()
    try:
        first = FinbertCache(cache_path=p)
        warm(first, ["a", "b"])

        second = FinbertCache(cache_path=p)
        model = warm(second, ["a", "b", "c", "d"])
        assert sorted(model.seen) == ["c", "d"], model.seen
    finally:
        shutil.rmtree(p.parent)


def test_a_corrupt_cache_costs_recomputation_not_the_run():
    p = tmp()
    try:
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(b"not parquet at all")
        cache = FinbertCache(cache_path=p)          # must not raise
        assert cache.scores == {}
        model = warm(cache, ["a"])
        assert model.seen == ["a"]
    finally:
        shutil.rmtree(p.parent)


def test_partial_progress_is_flushed_before_the_end():
    """A five-hour job that loses everything on an interruption is one nobody
    dares re-run, so the cache is written as it goes."""
    p = tmp()
    try:
        cache = FinbertCache(cache_path=p)
        warm(cache, [f"headline {i}" for i in range(3000)])
        assert p.exists()
        assert len(FinbertCache(cache_path=p).scores) == 3000
    finally:
        shutil.rmtree(p.parent)


# ---- keying -----------------------------------------------------------------

def test_key_is_exact_text_not_normalized():
    """FinBERT's answer depends on the string it was handed. A normalized key would
    hand back a score for text the model never actually saw."""
    assert key("Apple beats") != key("apple beats")
    assert key("Apple beats") != key("Apple  beats")
    assert key("Apple beats") == key("Apple beats")


def test_scoring_averages_only_what_is_known():
    p = tmp()
    try:
        cache = FinbertCache(cache_path=p)
        warm(cache, ["known"])
        assert cache.score(["known", "never seen"]) == cache.score(["known"])
        assert cache.score(["never seen"]) is None
        assert cache.score([]) is None
    finally:
        shutil.rmtree(p.parent)


def test_disabled_cache_with_no_history_produces_no_news_component():
    p = tmp()
    try:
        cache = FinbertCache(enabled=False, cache_path=p)
        assert cache.available is False
        assert cache.score(["anything"]) is None
    finally:
        shutil.rmtree(p.parent)


def test_a_warm_cache_is_usable_even_with_the_model_disabled():
    """Re-running with --no-finbert should still get news scores from history
    rather than silently dropping the component."""
    p = tmp()
    try:
        warm(FinbertCache(cache_path=p), ["a"])
        offline = FinbertCache(enabled=False, cache_path=p)
        assert offline.available is True
        assert offline.score(["a"]) is not None
    finally:
        shutil.rmtree(p.parent)


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
