# QuantWise — Gemini structured sentiment + event extraction (§ 1.6).
#
# Replaces FinBERT for markets whose news FinBERT cannot read (Arabic/EGX) and,
# eventually, for US too (one code path). One extraction serves two consumers:
#   • sentiment score → risk rules / future training feature
#   • event_tags      → the catalyst/IPO watchlist sleeve (§ 3.4)
#
# Design rules (same grounding philosophy as the recommendation LLM):
#   • Structured output enforced via responseSchema — the model cannot ramble.
#   • Fixed event-tag vocabulary — free-text tags would be unusable downstream.
#   • Deterministic-ish: temperature 0.
#   • Fail-closed: any error → None; callers treat it as "no sentiment signal",
#     exactly like FinBERT failures today.
#
# Key: GEMINI_API_KEY or Recommendations__Llm__ApiKey (same key the .NET
# backend uses; loaded from environment, never hard-coded).

from __future__ import annotations

import json
import logging
import os

import requests

log = logging.getLogger(__name__)

GEMINI_BASE = "https://generativelanguage.googleapis.com/v1beta"
MODEL = os.getenv("SENTIMENT_LLM_MODEL", "gemini-2.5-flash")

EVENT_TAGS = [
    "earnings", "product_launch", "govt_contract", "capital_increase",
    "mgmt_change", "regulatory", "macro", "mna", "dividend", "guidance",
]

RESPONSE_SCHEMA = {
    "type": "OBJECT",
    "properties": {
        "sentiment":  {"type": "NUMBER", "description": "-1 (very negative) .. 1 (very positive) for the stock"},
        "confidence": {"type": "NUMBER", "description": "0..1, how clear the signal is"},
        "event_tags": {"type": "ARRAY", "items": {"type": "STRING", "enum": EVENT_TAGS}},
        "summary":    {"type": "STRING", "description": "one line, max 20 words"},
    },
    "required": ["sentiment", "confidence", "event_tags", "summary"],
}

PROMPT = """You are a financial news analyst. Given news headlines about {ticker},
assess the aggregate sentiment FOR THE STOCK (not the world) and tag concrete events.

Rules:
- sentiment: -1..1. Mixed/unclear news → near 0 with low confidence.
- Only tag events actually present in the headlines; an empty tag list is normal.
- Headlines may be in English or Arabic; handle both natively.
- summary: one factual line, no advice, no numbers that are not in the headlines.

Headlines:
{headlines}"""


def _api_key() -> str | None:
    return (os.getenv("GEMINI_API_KEY") or os.getenv("Recommendations__Llm__ApiKey") or "").strip() or None


def extract_sentiment(ticker: str, headlines: list[str], timeout: int = 30) -> dict | None:
    """Returns {sentiment, confidence, event_tags, summary} or None on any failure."""
    key = _api_key()
    if not key or not headlines:
        return None

    body = {
        "contents": [{"parts": [{"text": PROMPT.format(
            ticker=ticker, headlines="\n".join(f"- {h}" for h in headlines[:30]))}]}],
        "generationConfig": {
            "temperature": 0,
            "responseMimeType": "application/json",
            "responseSchema": RESPONSE_SCHEMA,
        },
    }
    try:
        resp = requests.post(
            f"{GEMINI_BASE}/models/{MODEL}:generateContent",
            headers={"x-goog-api-key": key, "Content-Type": "application/json"},
            json=body,
            timeout=timeout,
        )
        if resp.status_code != 200:
            log.warning(f"{ticker}: Gemini sentiment HTTP {resp.status_code}")
            return None
        payload = resp.json()
        text = payload["candidates"][0]["content"]["parts"][0]["text"]
        out = json.loads(text)
    except Exception as e:
        log.warning(f"{ticker}: Gemini sentiment failed — {e}")
        return None

    # Schema is enforced server-side; still validate hard invariants locally.
    try:
        out["sentiment"] = max(-1.0, min(1.0, float(out["sentiment"])))
        out["confidence"] = max(0.0, min(1.0, float(out["confidence"])))
        out["event_tags"] = [t for t in out.get("event_tags", []) if t in EVENT_TAGS]
        out["summary"] = str(out.get("summary", ""))[:200]
    except Exception as e:
        log.warning(f"{ticker}: Gemini sentiment invalid payload — {e}")
        return None
    return out
