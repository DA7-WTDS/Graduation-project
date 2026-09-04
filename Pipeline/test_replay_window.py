# Tests for replay/window.py — how the replay window gets chosen (MVP_PLAN § C.2 rule 1).
# Standalone, no pytest, no network:  python test_replay_window.py

import json
import shutil
import tempfile
from datetime import date
from pathlib import Path

from replay import window as w


def registry(tmp: Path, slices):
    p = tmp / "registry.json"
    p.write_text(json.dumps([{"version": f"v{i}", "test_slice_from": s}
                             for i, s in enumerate(slices)]), encoding="utf-8")
    return p


def tmp():
    return Path(tempfile.mkdtemp(prefix="qw_window_"))


# ---- the lower bound comes from the model, not a constant --------------------

def test_boundary_is_read_from_the_registry():
    t = tmp()
    try:
        assert w.oos_boundary(registry(t, ["2024-12-31"])) == date(2024, 12, 31)
    finally:
        shutil.rmtree(t)


def test_a_promoted_model_moves_the_boundary():
    """Promotion retrains on a longer window, so the split boundary moves forward.
    Hard-coding it would leave the replay quietly measuring memorization."""
    t = tmp()
    try:
        assert w.oos_boundary(registry(t, ["2024-12-31", "2025-06-30"])) == date(2025, 6, 30)
    finally:
        shutil.rmtree(t)


def test_missing_registry_falls_back_rather_than_crashing():
    t = tmp()
    try:
        assert w.oos_boundary(t / "nope.json") == w.FALLBACK_OOS_START
    finally:
        shutil.rmtree(t)


# ---- the default window is the intersection ---------------------------------

def test_default_window_is_the_news_bearing_part_of_the_oos_era():
    t = tmp()
    try:
        reg = registry(t, ["2024-12-31"])
        start, end = w.default_corpus_window(today=date(2026, 9, 4), registry_path=reg)
        assert start == date(2025, 9, 4), start      # retention binds, not the boundary
        assert end == date(2026, 9, 4)
    finally:
        shutil.rmtree(t)


def test_the_boundary_wins_when_retention_reaches_further_back():
    """If news went back further than the split, fetching it would invite replaying
    dates the model trained on."""
    t = tmp()
    try:
        reg = registry(t, ["2026-01-01"])
        start, _ = w.default_corpus_window(today=date(2026, 9, 4), registry_path=reg)
        assert start == date(2026, 1, 1), start
    finally:
        shutil.rmtree(t)


# ---- resolving where a replay actually starts -------------------------------

def test_prefers_what_the_corpus_measured_over_what_retention_allows():
    """A vendor that kept 11 months should shorten the window, not leave a stretch
    of newsless dates nobody notices."""
    t = tmp()
    try:
        reg = registry(t, ["2024-12-31"])
        start, why = w.resolve_replay_start(
            {"news_coverage_starts": "2025-10-15T06:00:00+00:00"}, registry_path=reg)
        assert start == date(2025, 10, 15), start
        assert "first headline" in why
    finally:
        shutil.rmtree(t)


def test_clamps_a_corpus_that_reaches_before_the_boundary():
    t = tmp()
    try:
        reg = registry(t, ["2025-06-01"])
        start, why = w.resolve_replay_start(
            {"news_coverage_starts": "2025-01-02T06:00:00+00:00"}, registry_path=reg)
        assert start == date(2025, 6, 1), start
        assert "out-of-sample boundary" in why
    finally:
        shutil.rmtree(t)


def test_explicit_start_wins_but_is_flagged_when_it_predates_the_boundary():
    """Replaying the full OOS window is legitimate; doing it by accident is not."""
    t = tmp()
    try:
        reg = registry(t, ["2025-06-01"])
        start, why = w.resolve_replay_start({}, explicit=date(2024, 1, 1), registry_path=reg)
        assert start == date(2024, 1, 1)
        assert "training window" in why
    finally:
        shutil.rmtree(t)


def test_explicit_start_inside_the_oos_era_is_not_flagged():
    t = tmp()
    try:
        reg = registry(t, ["2024-12-31"])
        start, why = w.resolve_replay_start({}, explicit=date(2026, 1, 1), registry_path=reg)
        assert start == date(2026, 1, 1)
        assert "training window" not in why
    finally:
        shutil.rmtree(t)


def test_empty_manifest_falls_back_to_the_retention_horizon():
    t = tmp()
    try:
        reg = registry(t, ["2024-12-31"])
        start, why = w.resolve_replay_start({}, registry_path=reg)
        assert start >= w.oos_boundary(reg)
        assert "news horizon" in why
    finally:
        shutil.rmtree(t)


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
