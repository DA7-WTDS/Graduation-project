# QuantWise — market data access layer.
#
# One MarketDataProvider implementation per (market, vendor) pair. Everything
# vendor-specific (yfinance, Finnhub, a future licensed EGX feed) lives behind
# this interface; nothing outside markets/<market>/ may import a vendor SDK.
# See IMPLEMENTATION_PLAN.md § 0.1 for the interim stance and the licensed-data
# migration checklist this abstraction exists to serve.

from __future__ import annotations

import logging
from abc import ABC, abstractmethod
from pathlib import Path
from typing import TYPE_CHECKING, Any

import yaml

if TYPE_CHECKING:
    import pandas as pd

log = logging.getLogger(__name__)

MARKETS_DIR = Path(__file__).parent.parent / "markets"


class MarketDataProvider(ABC):
    """Vendor-neutral data access for one market (US, EGX, ...)."""

    market: str = "?"

    def __init__(self, config: dict[str, Any]):
        self.config = config

    # ---- required by the daily scoring flow ----

    @abstractmethod
    def get_universe(self) -> list[str]:
        """Investable ticker universe for this market (config-driven rules)."""

    @abstractmethod
    def get_ohlcv_batch(self, tickers: list[str], period: str | None = None) -> "pd.DataFrame | None":
        """Batch daily OHLCV for the whole universe (grouped by ticker).
        Fetch window comes from the market config; `period` overrides it
        (training uses long histories, serving uses the config default)."""

    @abstractmethod
    def get_closes(
        self, tickers: list[str], start: str, end: str
    ) -> dict[str, dict[str, float]]:
        """Historical closes for outcome scoring: {ticker: {ISO date: close}}.
        `start`/`end` are ISO dates (end exclusive, vendor-adjusted prices)."""

    @abstractmethod
    def gather_ticker_context(self, ticker: str) -> dict | None:
        """Analyst consensus + rating actions + price targets + news headlines
        for one ticker (network I/O phase of sentiment; safe in thread pools).
        Returns the dict consumed by main._score_gathered, or None on failure."""

    # ---- declared for the licensed-data migration (not used by the current flow) ----

    def get_corporate_actions(self, ticker: str):  # pragma: no cover
        raise NotImplementedError(f"{self.market}: corporate actions not implemented")

    def get_calendar(self):  # pragma: no cover
        raise NotImplementedError(f"{self.market}: trading calendar not implemented")


def load_market_config(market: str) -> dict[str, Any]:
    path = MARKETS_DIR / market / "config.yaml"
    if not path.exists():
        raise FileNotFoundError(f"No config for market '{market}' at {path}")
    with open(path, encoding="utf-8") as fh:
        return yaml.safe_load(fh) or {}


def get_provider(market: str = "us") -> MarketDataProvider:
    """Factory: returns the configured provider for an *enabled* market."""
    market = market.lower()
    config = load_market_config(market)

    if not config.get("enabled", False):
        raise RuntimeError(
            f"Market '{market}' is disabled in its config.yaml "
            "(EGX stays disabled until the licensed data adapter lands — "
            "see IMPLEMENTATION_PLAN.md § 0.1)."
        )

    if market == "us":
        from markets.us.provider import USMarketDataProvider

        return USMarketDataProvider(config)
    if market == "egx":
        from markets.egx.data import EGXMarketDataProvider

        return EGXMarketDataProvider(config)

    raise ValueError(f"Unknown market '{market}'")
