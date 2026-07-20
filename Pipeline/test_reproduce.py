# Tests for the § 6.3 reproduce endpoint's contract (IMPLEMENTATION_PLAN § 6.3).
# Standalone, no pytest:  python test_reproduce.py
#
# These cover validation and comparison logic only — they stub the inference core
# so they run in milliseconds without loading models. Real end-to-end
# reproducibility (predict -> snapshot -> replay -> identical) is verified against
# live market data; see the § 6.3 notes.

import sys

sys.path.insert(0, ".")

import main
from fastapi import HTTPException


def _stub_models(direction="UP", change_pct=2.5, confidence=0.9):
    """Bypass model loading; _infer is exercised for real in the live round-trip."""
    main._state.lstm = object()  # non-None so the 503 guard passes
    main._infer = lambda lw, tl: (direction, change_pct, confidence)


def snapshot(v=main.SNAPSHOT_SCHEMA, rows=None, tech=None):
    rows = rows if rows is not None else [[0.1] * len(main.FEATURE_COLS)] * main.LOOK_BACK
    tech = tech if tech is not None else [0.1] * len(main.TECH_COLS)
    return {"v": v, "lstm_window": rows, "tech_last": tech}


def expect_http(status, fn, *a, **kw):
    try:
        fn(*a, **kw)
    except HTTPException as e:
        assert e.status_code == status, f"expected {status}, got {e.status_code}: {e.detail}"
        return e.detail
    raise AssertionError(f"expected HTTP {status}, no exception raised")


def test_replays_and_reports_values():
    _stub_models()
    r = main.reproduce(main.ReproduceRequest(features=snapshot()))
    assert (r.direction, r.change_pct, r.confidence) == ("UP", 2.5, 0.9)
    # Nothing to compare against -> no verdict, rather than a misleading "True".
    assert r.matches is None and r.mismatches == []


def test_reports_match_when_stored_values_agree():
    _stub_models()
    r = main.reproduce(main.ReproduceRequest(
        features=snapshot(), expected_direction="UP",
        expected_change_pct=2.5, expected_confidence=0.9))
    assert r.matches is True and r.mismatches == []


def test_reports_each_drifting_field():
    _stub_models()
    r = main.reproduce(main.ReproduceRequest(
        features=snapshot(), expected_direction="DOWN",
        expected_change_pct=-1.0, expected_confidence=0.1))
    assert r.matches is False
    assert len(r.mismatches) == 3
    assert any("direction" in m for m in r.mismatches)


def test_tolerates_last_place_rounding():
    _stub_models(change_pct=2.5)
    r = main.reproduce(main.ReproduceRequest(
        features=snapshot(), expected_change_pct=2.50005))
    assert r.matches is True


def test_artifact_drift_is_reported_not_rejected():
    # Replaying an old prediction under today's artifacts is a legitimate audit:
    # it is how you show what a model change did. It must not 4xx.
    _stub_models()
    r = main.reproduce(main.ReproduceRequest(
        features=snapshot(), model_version="stale", scaler_hash="stale"))
    assert r.model_version_matches is False
    assert r.scaler_hash_matches is False
    assert r.model_version == main.MODEL_VERSION

    unknown = main.reproduce(main.ReproduceRequest(features=snapshot()))
    assert unknown.model_version_matches is None  # not asked, not guessed


def test_rejects_unknown_schema_version():
    _stub_models()
    detail = expect_http(400, main.reproduce, main.ReproduceRequest(features=snapshot(v=99)))
    assert "schema" in detail


def test_rejects_wrong_window_shape():
    _stub_models()
    short = snapshot(rows=[[0.1] * len(main.FEATURE_COLS)] * (main.LOOK_BACK - 1))
    detail = expect_http(400, main.reproduce, main.ReproduceRequest(features=short))
    assert "lstm_window" in detail


def test_rejects_wrong_tech_width():
    _stub_models()
    detail = expect_http(400, main.reproduce,
                         main.ReproduceRequest(features=snapshot(tech=[0.1, 0.2])))
    assert "tech_last" in detail


def test_rejects_malformed_snapshot():
    _stub_models()
    expect_http(400, main.reproduce,
                main.ReproduceRequest(features={"v": main.SNAPSHOT_SCHEMA}))


def test_503_when_models_not_loaded():
    main._state.lstm = None
    expect_http(503, main.reproduce, main.ReproduceRequest(features=snapshot()))


def test_model_identity_is_content_addressed():
    # Derived from the artifacts themselves, so it cannot drift out of sync with
    # a hand-maintained version string.
    assert len(main.MODEL_VERSION) == 16 and len(main.SCALER_HASH) == 16
    assert main.MODEL_VERSION != main.SCALER_HASH


if __name__ == "__main__":
    _real_infer = main._infer
    failures = 0
    for name, fn in sorted({k: v for k, v in globals().items() if k.startswith("test_")}.items()):
        main._infer = _real_infer  # reset any stub between tests
        try:
            fn()
            print(f"PASS {name}")
        except AssertionError as e:
            failures += 1
            print(f"FAIL {name}: {e}")
    raise SystemExit(failures)
