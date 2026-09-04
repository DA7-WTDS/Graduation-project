# Tests for risk_rules.py, with a focus on the rank-based low_conviction flag
# (MVP_PLAN § A follow-up 1). Standalone, no pytest:  python test_risk_rules.py

import risk_rules as rr


def pred(ticker, change_pct=5.0, confidence=0.9, direction=None):
    return {
        "ticker": ticker,
        "direction": direction or ("UP" if change_pct >= 0 else "DOWN"),
        "change_pct": change_pct,
        "confidence": confidence,
    }


def _k(n):
    """How many names a leg selects from a run of `n` with no ties."""
    return int(n * rr.LOW_CONVICTION_QUANTILE)


def run(preds, sents=None):
    return {r["ticker"]: r for r in rr.apply_risk_rules(preds, sents or [])}


def flagged(out):
    return {t for t, r in out.items() if "low_conviction" in r["risk_flags"]}


# ---- the scale bug this replaced --------------------------------------------

def test_relative_scale_does_not_flag_the_whole_run():
    """The trees champion emits change_pct in tenths of a percent. The old absolute
    cutoff (< 1.5) flagged 95.9% of such a run; ranking must not."""
    preds = [pred(f"T{i:02d}", change_pct=0.1 + i * 0.01, confidence=0.48 + i * 0.0005)
             for i in range(100)]
    out = run(preds)
    assert all(abs(p["change_pct"]) < rr.LOW_CONVICTION_PCT for p in preds), "fixture must sit under the old cutoff"
    assert len(flagged(out)) <= 15, f"flagged {len(flagged(out))}/100"


def test_squashed_confidence_still_flags_someone():
    """Calibrated confidence is floored around 0.458, so the old < 0.30 cutoff fired on
    nobody. Ranking must still single out the weakest names."""
    preds = [pred(f"T{i:02d}", change_pct=1.0 + i, confidence=0.46 + i * 0.001) for i in range(100)]
    out = run(preds)
    assert all(p["confidence"] > rr.LOW_CONFIDENCE for p in preds), "fixture must clear the old cutoff"
    assert len(flagged(out)) > 0


# ---- what the flag now means ------------------------------------------------

def test_flags_the_weakest_by_confidence():
    preds = [pred(f"T{i:02d}", change_pct=5.0, confidence=0.50 + i * 0.001) for i in range(100)]
    assert flagged(run(preds)) == {f"T{i:02d}" for i in range(_k(100))}


def test_flags_the_weakest_by_absolute_score():
    # Confidence identical, so only the |change_pct| leg can select anyone.
    preds = [pred(f"T{i:02d}", change_pct=(i + 1) * 0.1, confidence=0.5) for i in range(100)]
    assert flagged(run(preds)) == {f"T{i:02d}" for i in range(_k(100))}


def test_ranks_on_magnitude_not_sign():
    """A strong DOWN call is a strong call. Only names near zero are undifferentiated."""
    preds = [pred("BIGDOWN", change_pct=-9.0, confidence=0.9),
             pred("FLAT", change_pct=0.001, confidence=0.9)]
    preds += [pred(f"T{i:02d}", change_pct=5.0 + i, confidence=0.9) for i in range(40)]
    out = run(preds)
    assert "FLAT" in flagged(out)
    assert "BIGDOWN" not in flagged(out)


def test_a_fully_tied_run_flags_nobody():
    """Every name identical: none is weaker than the others, so none is flagged.
    Selecting a fixed 6% here would be picking names by alphabet."""
    preds = [pred(f"T{i:02d}", change_pct=3.0, confidence=0.484) for i in range(100)]
    assert flagged(run(preds)) == set()


def test_ties_at_the_cutoff_are_not_split():
    """A tied block straddling the cutoff is excluded whole, never half-flagged."""
    # 10 names at the floor, 90 clearly stronger: the cutoff lands inside the tied block.
    preds = ([pred(f"L{i:02d}", change_pct=1.0, confidence=0.458) for i in range(10)] +
             [pred(f"H{i:02d}", change_pct=5.0 + i, confidence=0.9) for i in range(90)])
    out = flagged(run(preds))
    assert not any(t.startswith("H") for t in out)
    # Either all ten tied names or none of them — never a subset chosen by name.
    lows = {t for t in out if t.startswith("L")}
    assert lows in (set(), {f"L{i:02d}" for i in range(10)})


def test_scale_invariance():
    """Multiplying every score by 100 — a champion promotion changing the score's scale "
    must not change which names are flagged. This is why ranking replaced constants."""
    small = [pred(f"T{i:02d}", change_pct=(i + 1) * 0.01, confidence=0.48 + i * 0.0001) for i in range(100)]
    large = [dict(p, change_pct=p["change_pct"] * 100) for p in small]
    assert flagged(run(small)) == flagged(run(large))


# ---- fallback + untouched behaviour -----------------------------------------

def test_single_record_falls_back_to_absolute_thresholds():
    """A decile is meaningless on a handful of records, so the absolute cutoffs stand."""
    weak = rr.enrich_record(pred("AAA", change_pct=0.5, confidence=0.9))
    assert "low_conviction" in weak["risk_flags"]
    strong = rr.enrich_record(pred("AAA", change_pct=9.0, confidence=0.9))
    assert "low_conviction" not in strong["risk_flags"]


def test_explicit_flag_overrides_the_absolute_cutoffs():
    r = rr.enrich_record(pred("AAA", change_pct=0.5, confidence=0.9), low_conviction=False)
    assert "low_conviction" not in r["risk_flags"]


def test_other_flags_are_unchanged():
    out = run([pred("EXTREME", change_pct=20.0, confidence=0.9)] +
              [pred(f"T{i:02d}", change_pct=5.0 + i, confidence=0.9) for i in range(40)])
    assert "extreme_move" in out["EXTREME"]["risk_flags"]
    assert "thin_coverage" in out["EXTREME"]["risk_flags"]   # no sentiment supplied
    assert "stale_analyst" in out["EXTREME"]["risk_flags"]


def test_min_records_guard_still_aborts():
    try:
        rr.apply_risk_rules([pred("AAA")], [])
    except ValueError as e:
        assert "only 1 records" in str(e)
    else:
        raise AssertionError("expected ValueError")


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
