# markets/egx/data.py
# TODO(EGX-DATA): EGX data adapter intentionally not implemented.
# Awaiting licensed EOD feed (OHLCV + corporate actions + trading calendar).
# Implement against the MarketDataProvider interface below when licensing lands.
# See IMPLEMENTATION_PLAN.md § 0.1 for the migration checklist.

from __future__ import annotations

from core.data_provider import MarketDataProvider

_NOT_IMPLEMENTED = (
    "EGX data adapter is not implemented yet — awaiting licensed EOD feed. "
    "See TODO(EGX-DATA) in markets/egx/data.py and IMPLEMENTATION_PLAN.md § 0.1."
)


class EGXMarketDataProvider(MarketDataProvider):
    """Scaffold. Every method raises until the licensed adapter is written.
    The market is additionally gated by markets/egx/config.yaml (enabled: false),
    so get_provider('egx') refuses before this class is ever reached."""

    market = "egx"

    def get_universe(self) -> list[str]:
        raise NotImplementedError(_NOT_IMPLEMENTED)

    def get_ohlcv_batch(self, tickers):
        raise NotImplementedError(_NOT_IMPLEMENTED)

    def get_closes(self, tickers, start, end):
        raise NotImplementedError(_NOT_IMPLEMENTED)

    def gather_ticker_context(self, ticker):
        raise NotImplementedError(_NOT_IMPLEMENTED)
