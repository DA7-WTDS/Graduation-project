# Tests for core/sentiment_scoring.py — the composite shared by live scoring and
# point-in-time replay (MVP_PLAN § C.2 rule 3).
# Standalone, no pytest:  python test_sentiment_scoring.py

from core import sentiment_scoring as ss


# The pre-extraction arithmetic from main._score_gathered, reimplemented here
# independently. If someone "simplifies" the shared module and changes what a
# score means, this fails — which is the whole reason live and replay were allowed
# to share one implementation in the first place.
def reference(avg, action_score, pt_up, news):
    consensus = (avg - 3.0) / 2.0 if avg is not None else None
    pt = max(-1.0, min(1.0, pt_up / 25.0)) if pt_up is not None else None
    weights = {"consensus": 0.40, "actions": 0.15, "price_target": 0.20, "news": 0.25}

    parts = {}
    if consensus is not None:
        parts["consensus"] = round(consensus, 3)
    if action_score is not None:
        parts["actions"] = round(action_score, 3)
    if pt is not None:
        parts["price_target"] = round(pt, 3)
    if news is not None:
        parts["news"] = round(news, 3)

    if parts:
        wsum = sum(weights[k] for k in parts)
        score = round(sum(parts[k] * weights[k] for k in parts) / wsum, 3)
    else:
        score = 0.0

    signal = "POSITIVE" if score > 0.15 else "NEGATIVE" if score < -0.15 else "NEUTRAL"
    return score, signal, parts


CASES = [
    (4.3, 0.55, 19.05, None),        # the live smoke case, FinBERT unavailable
    (4.3, 0.55, 19.05, 0.31),        # everything present
    (None, None, None, None),        # nothing at all
    (3.0, 0.0, 0.0, 0.0),            # dead neutral
    (1.0, -1.0, -80.0, -0.9),        # maximally negative, PT beyond the clamp
    (5.0, 1.0, 120.0, 0.9),          # maximally positive, PT beyond the clamp
    (None, 0.4, None, 0.2),          # no analyst coverage
    (4.0, None, None, None),         # consensus only
    (None, None, None, 0.5),         # news only
]


def test_matches_the_original_arithmetic_exactly():
    for avg, actions, pt_up, news in CASES:
        expected = reference(avg, actions, pt_up, news)
        actual = ss.composite(
            consensus=ss.consensus_score(avg),
            actions=actions,
            price_target=ss.price_target_score(pt_up),
            news=news,
        )
        assert actual == expected, f"{(avg, actions, pt_up, news)}: {actual} != {expected}"


def test_missing_components_reweight_rather_than_score_zero():
    """A missing block must not be treated as a neutral opinion — that would drag
    every thin-coverage name toward 0 and make them look like considered holds."""
    only_news, _, parts = ss.composite(news=0.8)
    assert only_news == 0.8 and parts == {"news": 0.8}

    # Same components, one absent: the survivors carry full weight between them.
    both, _, _ = ss.composite(consensus=0.8, news=0.8)
    assert both == 0.8


def test_no_components_is_neutral_not_negative():
    score, signal, parts = ss.composite()
    assert (score, signal, parts) == (0.0, "NEUTRAL", {})


def test_price_target_is_clamped_both_ways():
    assert ss.price_target_score(250.0) == 1.0
    assert ss.price_target_score(-250.0) == -1.0
    assert ss.price_target_score(25.0) == 1.0
    assert ss.price_target_score(None) is None


def test_price_target_can_be_excluded_for_replay():
    """§ C.2 rule 3: vendors only expose the CURRENT target, so replay must drop it.
    Dropping it has to reweight, not zero-fill."""
    live, _, live_parts = ss.composite(consensus=0.6, actions=0.5, price_target=0.9, news=0.2)
    replayed, _, replay_parts = ss.composite(consensus=0.6, actions=0.5, news=0.2)
    assert "price_target" in live_parts and "price_target" not in replay_parts
    assert replayed != live
    # Renormalized over the three survivors, not scaled down by the missing weight.
    expected = round((0.6 * 0.40 + 0.5 * 0.15 + 0.2 * 0.25) / (0.40 + 0.15 + 0.25), 3)
    assert replayed == expected


def test_consensus_maps_hold_to_zero():
    assert ss.consensus_score(3.0) == 0.0
    assert ss.consensus_score(5.0) == 1.0
    assert ss.consensus_score(1.0) == -1.0
    assert ss.consensus_score(None) is None


def test_label_boundaries_are_exclusive():
    assert ss.label(0.15) == "NEUTRAL"      # exactly on the threshold is not positive
    assert ss.label(0.151) == "POSITIVE"
    assert ss.label(-0.15) == "NEUTRAL"
    assert ss.label(-0.151) == "NEGATIVE"


def test_news_score_from_finbert():
    batch = [
        [{"label": "positive", "score": 0.9}, {"label": "negative", "score": 0.05}],
        [{"label": "positive", "score": 0.1}, {"label": "negative", "score": 0.8}],
    ]
    # mean of (0.85, -0.70)
    assert ss.news_score_from_finbert(batch) == 0.075
    assert ss.news_score_from_finbert([]) is None


def test_finbert_labels_are_case_insensitive():
    """Model cards have shipped both 'positive' and 'POSITIVE'; a silent 0.0 here
    would look exactly like genuinely neutral news."""
    upper = [[{"label": "POSITIVE", "score": 0.7}, {"label": "NEGATIVE", "score": 0.1}]]
    assert ss.news_score_from_finbert(upper) == 0.6


if __name__ == "__main__":
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    for t in tests:
        t()
        print("PASS", t.__name__)
    print(f"{len(tests)} passed")
