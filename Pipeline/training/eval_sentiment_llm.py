# QuantWise — golden-set eval for the Gemini sentiment extractor (§ 1.6 / § 3.6).
#
# The eval-harness principle: every prompt or model change must pass these
# fixed cases before shipping. Asserts schema validity, directional sanity on
# unambiguous news (EN + AR), and fail-closed behavior. Run:
#
#   python -m training.eval_sentiment_llm
#
# Skips gracefully (exit 0, SKIPPED) when no Gemini key is in the environment;
# also loads the repo-root .env so the backend's key is reused without copying.

from __future__ import annotations

import logging
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)


def load_dotenv_upward() -> None:
    """Minimal .env loader (no python-dotenv dep): merge every .env walking up
    (nearest wins via setdefault; Pipeline/.env AND the repo-root .env both load)."""
    d = Path(__file__).resolve()
    for parent in d.parents:
        env = parent / ".env"
        if not env.exists():
            continue
        for line in env.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, _, v = line.partition("=")
            os.environ.setdefault(k.strip(), v.strip().strip('"'))


GOLDEN = [
    {
        "name": "clearly_positive_en",
        "ticker": "MSFT",
        "headlines": [
            "Microsoft crushes earnings expectations, raises full-year guidance",
            "Microsoft announces breakthrough AI product line, analysts cheer",
            "Microsoft dividend hiked 10% after record quarter",
        ],
        "check": lambda r: r["sentiment"] > 0.2 and ({"earnings", "product_launch", "dividend", "guidance"} & set(r["event_tags"])),
    },
    {
        "name": "clearly_negative_en",
        "ticker": "ACME",
        "headlines": [
            "ACME under federal fraud investigation, shares plunge",
            "ACME slashes guidance amid accounting scandal",
            "ACME CFO resigns abruptly as auditors raise doubts",
        ],
        "check": lambda r: r["sentiment"] < -0.2,
    },
    {
        "name": "mixed_neutral_en",
        "ticker": "JPM",
        "headlines": [
            "JPMorgan opens new branch in Ohio",
            "Analysts split on JPMorgan outlook for next quarter",
            "JPMorgan sponsors community art festival",
        ],
        "check": lambda r: abs(r["sentiment"]) <= 0.6,
    },
    {
        "name": "clearly_positive_ar",
        "ticker": "COMI.CA",
        "headlines": [
            "البنك التجاري الدولي يحقق أرباحاً قياسية ويرفع توزيعات الأسهم",
            "نمو أرباح البنك التجاري الدولي 40% متجاوزاً التوقعات",
        ],
        "check": lambda r: r["sentiment"] > 0.2,
    },
    {
        "name": "clearly_negative_ar",
        "ticker": "XYZ.CA",
        "headlines": [
            "الرقابة المالية تحقق مع الشركة في شبهات تلاعب بالقوائم المالية",
            "الشركة تخفض توقعات الأرباح وسط أزمة سيولة خانقة",
        ],
        "check": lambda r: r["sentiment"] < -0.2,
    },
]


def main() -> int:
    load_dotenv_upward()
    from core.sentiment_llm import extract_sentiment, _api_key

    if _api_key() is None:
        log.warning("SKIPPED — no GEMINI_API_KEY / Recommendations__Llm__ApiKey in environment.")
        return 0

    # Fail-closed case needs no API: empty headlines must return None.
    assert extract_sentiment("MSFT", []) is None, "empty headlines must return None"

    passed = failed = 0
    for case in GOLDEN:
        r = extract_sentiment(case["ticker"], case["headlines"])
        ok = r is not None and bool(case["check"](r))
        status = "PASS" if ok else "FAIL"
        detail = f"sent={r['sentiment']:+.2f} conf={r['confidence']:.2f} tags={r['event_tags']}" if r else "None"
        log.info(f"{status}  {case['name']:22s} {detail}")
        passed += ok
        failed += (not ok)

    log.info(f"Golden eval: {passed}/{len(GOLDEN)} passed")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
