# Tests for replay/ingest_runs.py — the record mapping that crosses the wire into
# the backend's PredictionRecordDto. Standalone, no pytest, no network:
#   python test_ingest_runs.py
#
# A mapping fault here is not a crash; it is a silently wrong portfolio. A null
# where the DTO wants a string, or NaN where it wants a number, either rejects a
# whole date or lands a record the optimizer then weights.

import json
import math

import pandas as pd

from replay import ingest_runs as ir


def row(**over):
    base = {
        "ticker": "AAPL", "direction": "UP", "change_pct": 1.23, "confidence": 0.55,
        "sentiment_score": 0.4, "signal": "POSITIVE", "analyst_rating": 4.2,
        "rating_label": "Buy", "pt_upside_pct": None, "news_score": 0.3,
        "agreement": "CONFIRMED", "risk_level": "LOW", "conviction_score": 0.64,
        "risk_flags": ["signal_confirmed"], "rationale": "UP +1.23%",
        "rsi_14": 55.0, "pct_vs_sma50": 0.02,
        "features": '{"v": 1, "mode": "trees", "tech_last": [0.1]}',
        "model_version": "abc123", "scaler_hash": None,
    }
    base.update(over)
    return pd.Series(base)


def test_produces_every_field_the_dto_expects():
    got = ir.to_record(row())
    assert set(got) == set(ir.RECORD_FIELDS), set(ir.RECORD_FIELDS) ^ set(got)


def test_payload_is_json_serialisable():
    """pandas types (numpy floats, arrays, Timestamps) are not JSON by default, and
    the failure would reject an entire replayed date."""
    payload = json.dumps(ir.to_record(row()))
    assert "AAPL" in payload


def test_nan_becomes_null_not_the_string_nan():
    """json.dumps writes bare NaN, which is invalid JSON. The backend would reject
    the whole date rather than one record."""
    got = ir.to_record(row(analyst_rating=float("nan"), rsi_14=float("nan")))
    assert got["analyst_rating"] is None and got["rsi_14"] is None
    assert "NaN" not in json.dumps(got)


def test_features_json_is_sent_as_an_object_not_a_string():
    """The DTO types features as JsonElement. A string would store a quoted blob
    and break /api/reproduce, which is the audit path."""
    got = ir.to_record(row())
    assert isinstance(got["features"], dict)
    assert got["features"]["mode"] == "trees"


def test_unparseable_features_degrade_to_null_rather_than_failing_the_date():
    got = ir.to_record(row(features="{not json"))
    assert got["features"] is None


def test_required_strings_never_go_null():
    """These are non-nullable on the DTO; a null makes the backend reject the batch."""
    got = ir.to_record(row(rationale=None, signal=None, agreement=None))
    for field in ("rationale", "signal", "agreement", "ticker", "direction", "risk_level"):
        assert isinstance(got[field], str), field


def test_risk_flags_is_always_a_list():
    assert ir.to_record(row(risk_flags=None))["risk_flags"] == []
    assert ir.to_record(row(risk_flags=["a", "b"]))["risk_flags"] == ["a", "b"]


def test_numpy_arrays_are_converted():
    import numpy as np
    got = ir.to_record(row(risk_flags=np.array(["extreme_move"])))
    assert got["risk_flags"] == ["extreme_move"]
    json.dumps(got)


def test_price_targets_stay_null_across_the_wire():
    """§ C.2 rule 3. The scorer never populates it; the mapping must not invent it."""
    assert ir.to_record(row())["pt_upside_pct"] is None


def test_optional_nulls_are_preserved():
    got = ir.to_record(row(scaler_hash=None, news_score=None))
    assert got["scaler_hash"] is None and got["news_score"] is None


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
