# QuantWise — persistent FinBERT score cache.
#
# Scoring the replay corpus costs ~5 hours of CPU for ~98k unique headlines. Held
# only in process memory, that work is thrown away on exit, so every re-run — a
# widened universe, a corrected window, a crash at hour four — pays it again.
#
# Headline text is immutable and FinBERT is deterministic, so a score is a pure
# function of the string. That makes it cacheable forever, keyed by a hash of the
# text. The cache is therefore not an optimization detail; it is what makes the
# replay re-runnable at all.
#
# Saved incrementally rather than at the end. A five-hour job that loses
# everything on an interruption is a job nobody dares re-run.

from __future__ import annotations

import hashlib
import logging
import os
from pathlib import Path

log = logging.getLogger(__name__)

BASE_DIR = Path(__file__).parent.parent
CACHE_PATH = Path(os.getenv("FINBERT_CACHE_PATH") or (BASE_DIR / "training" / "data" / "finbert_cache.parquet"))

# How often to flush while warming. Small enough that an interruption costs
# minutes, large enough that the write is not the bottleneck.
FLUSH_EVERY = 2000


def key(headline: str) -> str:
    """Cache key: a hash of the exact text. Not normalized — FinBERT's answer
    depends on the string it was given, so a 'cleaned' key would return a score
    for text the model never actually saw."""
    return hashlib.sha1(headline.encode("utf-8")).hexdigest()[:20]


class FinbertCache:
    """FinBERT scores, remembered across runs.

    The model is loaded lazily and only if something actually needs scoring: a
    fully warm cache means a replay never pays the ~30s model load, let alone the
    inference.
    """

    def __init__(self, enabled: bool = True, cache_path: Path | None = None):
        self.enabled = enabled
        self.path = Path(cache_path) if cache_path is not None else CACHE_PATH
        self.scores: dict[str, float] = {}
        self.model = None
        self._loaded_from_disk = 0
        self._load()

    # ---- persistence ----

    def _load(self) -> None:
        if not self.path.exists():
            return
        try:
            import pandas as pd
            frame = pd.read_parquet(self.path)
            self.scores = dict(zip(frame["key"], frame["score"]))
            self._loaded_from_disk = len(self.scores)
            log.info(f"FinBERT cache: {len(self.scores):,} scores loaded from {self.path.name}")
        except Exception as e:
            # A corrupt cache must not stop a replay; it only costs recomputation.
            log.warning(f"FinBERT cache unreadable, starting empty — {e}")
            self.scores = {}

    def save(self) -> None:
        if not self.scores:
            return
        import pandas as pd
        self.path.parent.mkdir(parents=True, exist_ok=True)
        tmp = self.path.with_suffix(".parquet.tmp")
        pd.DataFrame({"key": list(self.scores), "score": list(self.scores.values())}).to_parquet(tmp, index=False)
        tmp.replace(self.path)   # atomic: a crash mid-write cannot corrupt the cache

    # ---- scoring ----

    def _ensure_model(self) -> bool:
        if self.model is not None:
            return True
        if not self.enabled:
            return False
        try:
            from transformers import pipeline as hf_pipeline
            self.model = hf_pipeline("text-classification", model="ProsusAI/finbert", top_k=None)
            log.info("FinBERT loaded.")
            return True
        except Exception as e:
            log.error(f"FinBERT unavailable, replay runs without a news component — {e}")
            return False

    def warm(self, headlines: set[str], batch_size: int = 32) -> dict[str, int]:
        """Score whatever is not already cached. Returns {requested, cached, scored}."""
        stats = {"requested": len(headlines), "cached": 0, "scored": 0}
        if not headlines:
            return stats

        todo = sorted(h for h in headlines if key(h) not in self.scores)
        stats["cached"] = len(headlines) - len(todo)
        if not todo:
            log.info(f"FinBERT cache: all {len(headlines):,} headlines already scored, nothing to do.")
            return stats

        if not self._ensure_model():
            return stats

        log.info(f"FinBERT: {len(todo):,} to score ({stats['cached']:,} already cached).")
        since_flush = 0
        for i in range(0, len(todo), 512):
            chunk = todo[i:i + 512]
            outs = self.model(chunk, truncation=True, max_length=128, batch_size=batch_size)
            for headline, out in zip(chunk, outs):
                probs = {x["label"].lower(): x["score"] for x in out}
                self.scores[key(headline)] = probs.get("positive", 0.0) - probs.get("negative", 0.0)
            stats["scored"] += len(chunk)
            since_flush += len(chunk)
            if since_flush >= FLUSH_EVERY:
                self.save()
                since_flush = 0
            log.info(f"    {min(i + 512, len(todo)):,}/{len(todo):,}")

        self.save()
        return stats

    def score(self, headlines: list[str]) -> float | None:
        """Mean score over the headlines that are cached. None when none are."""
        vals = [self.scores[k] for k in (key(h) for h in headlines) if k in self.scores]
        if not vals:
            return None
        return round(sum(vals) / len(vals), 3)

    @property
    def available(self) -> bool:
        """Whether a news component can be produced at all."""
        return bool(self.scores) or self.enabled
