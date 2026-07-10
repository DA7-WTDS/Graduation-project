# QuantWise — US market data provider (interim vendors: yfinance + Finnhub).
#
# INTERIM STANCE (IMPLEMENTATION_PLAN.md § 0.1): yfinance + Finnhub carry the US
# market for development and early operation. They are adapters behind
# MarketDataProvider — swap by implementing a new provider, not by editing
# callers. All vendor code moved here from main.py unchanged in behavior.

from __future__ import annotations

import logging
import os
import re
import threading
import time
from datetime import datetime, timedelta, timezone

import pandas as pd
import requests
import yfinance as yf

from core.data_provider import MarketDataProvider

log = logging.getLogger(__name__)

# ---- yfinance global config (was module-level in main.py) ----

yf.config.network.retries = int(os.getenv("YF_RETRIES", "3"))
_YF_PROXY = os.getenv("YF_PROXY") or os.getenv("HTTPS_PROXY")
if _YF_PROXY:
    yf.config.network.proxy = _YF_PROXY
    log.info("yfinance proxy enabled.")
_YF_CACHE_DIR = os.getenv("YF_CACHE_DIR")
if _YF_CACHE_DIR:
    try:
        yf.set_tz_cache_location(_YF_CACHE_DIR)
        log.info(f"yfinance tz/cookie cache -> {_YF_CACHE_DIR}")
    except Exception as e:
        log.warning(f"Could not set yfinance cache location: {e}")

YF_MIN_INTERVAL = float(os.getenv("YF_MIN_INTERVAL", "0.3"))
_yf_throttle_lock = threading.Lock()
_yf_last_call = [0.0]  # mutable float container; guarded by _yf_throttle_lock


def _yf_throttle():
    """Space out Yahoo calls so bursts don't trip rate limits / IP bans."""
    if YF_MIN_INTERVAL <= 0:
        return
    with _yf_throttle_lock:
        wait = YF_MIN_INTERVAL - (time.time() - _yf_last_call[0])
        if wait > 0:
            time.sleep(wait)
        _yf_last_call[0] = time.time()


# ---- Finnhub REST primitives ----

FINNHUB_API_KEY = os.getenv("FINNHUB_API_KEY", "").strip()
FINNHUB_BASE = "https://finnhub.io/api/v1"
FINNHUB_NEWS_DAYS = 14
FINNHUB_FETCH_MAX = 150
FINNHUB_MIN_INTERVAL = 1.05  # stays under 60/min free limit

_finnhub_lock = threading.Lock()
_finnhub_last = [0.0]


def _finnhub_throttle():
    with _finnhub_lock:
        wait = FINNHUB_MIN_INTERVAL - (time.time() - _finnhub_last[0])
        if wait > 0:
            time.sleep(wait)
        _finnhub_last[0] = time.time()


def _finnhub_get(path: str, params: dict):
    if not FINNHUB_API_KEY:
        return None
    try:
        _finnhub_throttle()
        resp = requests.get(
            f"{FINNHUB_BASE}{path}",
            params={**params, "token": FINNHUB_API_KEY},
            timeout=15,
        )
        if resp.status_code != 200:
            log.warning(f"Finnhub {path} HTTP {resp.status_code}")
            return None
        return resp.json()
    except Exception as e:
        log.warning(f"Finnhub {path} failed — {e}")
        return None


# ---- universe (screener + fallback) ----

_FOREIGN_ADR_DENYLIST = {
    "HSBC", "AZN", "NVS", "SHEL", "BHP", "RIO", "TTE", "BUD", "UBS",
    "BP", "DEO", "BTI", "GSK", "SAP", "SNY", "UL", "NGG", "E", "TEF",
    "TSM", "BABA", "ASML", "NVO", "SONY", "TM", "PDD", "SPOT",
}

_FALLBACK_TICKERS = [
    "AAPL", "NVDA", "MSFT", "AMZN", "GOOGL", "GOOG", "META", "TSLA",
    "AVGO", "BRK-B", "JPM", "V", "MA", "BAC", "WFC", "GS", "MS",
    "AXP", "BLK", "LLY", "UNH", "JNJ", "ABBV", "MRK", "TMO", "ABT", "DHR",
    "ISRG", "PFE", "WMT", "COST", "PG", "KO", "PEP", "MCD", "NKE", "SBUX",
    "TGT", "HD", "XOM", "CVX", "COP", "SLB", "EOG", "MPC", "PSX", "VLO",
    "OXY", "HAL", "CAT", "DE", "HON", "UPS", "RTX", "LMT", "GE", "MMM",
    "BA", "FDX", "AMD", "INTC", "QCOM", "TXN", "MU", "AMAT", "LRCX", "KLAC",
    "MRVL", "ARM", "CRM", "ORCL", "NOW", "ADBE", "INTU", "PANW", "SNOW",
    "PLTR", "UBER", "ABNB", "NFLX", "DIS", "CMCSA", "T", "VZ", "TMUS",
    "CHTR", "WBD", "FOX", "PYPL", "SHOP", "COIN", "MSTR",
    "AMT", "PLD", "SPG", "O", "WELL",
]

# ---- sentiment-gather helpers (analyst + news; vendor parsing) ----

SENTIMENT_WINDOW_DAYS = 30
NEWS_LIMIT = 25
NEWS_MIN_RELEVANT = 3

_NAME_STOPWORDS = {
    "the", "inc", "co", "corp", "corporation", "company", "ltd",
    "plc", "group", "holdings", "com", "class", "incorporated", "llc", "sa", "nv", "ag", "and",
}
_GRADE_MAP = {
    "strong buy": 1.0, "conviction buy": 1.0, "buy": 0.6, "outperform": 0.6,
    "overweight": 0.6, "accumulate": 0.5, "add": 0.5, "positive": 0.6,
    "market outperform": 0.6, "sector outperform": 0.6, "long-term buy": 0.5,
    "hold": 0.0, "neutral": 0.0, "equal-weight": 0.0, "equalweight": 0.0,
    "market perform": 0.0, "sector perform": 0.0, "in-line": 0.0, "peer perform": 0.0,
    "reduce": -0.5, "sell": -0.6, "underperform": -0.6, "underweight": -0.6,
    "negative": -0.6, "market underperform": -0.6, "sector underperform": -0.6,
    "strong sell": -1.0,
}
_ACTION_LABEL = {"up": "upgrade", "down": "downgrade", "init": "initiated",
                 "main": "maintained", "reit": "reiterated"}


def _rating_label(avg: float) -> str:
    if avg >= 4.5: return "Strong Buy"
    if avg >= 3.5: return "Buy"
    if avg >= 2.5: return "Hold"
    if avg >= 1.5: return "Sell"
    return "Strong Sell"


def _finnhub_recommendation(ticker: str):
    data = _finnhub_get("/stock/recommendation", {"symbol": ticker})
    if not isinstance(data, list) or not data:
        return None
    row = data[0]
    sb = float(row.get("strongBuy") or 0); b  = float(row.get("buy") or 0)
    h  = float(row.get("hold") or 0);      s  = float(row.get("sell") or 0)
    ss = float(row.get("strongSell") or 0)
    n  = sb + b + h + s + ss
    if n <= 0:
        return None
    avg = (5 * sb + 4 * b + 3 * h + 2 * s + 1 * ss) / n
    return round(avg, 2), _rating_label(avg), int(n)


def _finnhub_profile_name(ticker: str) -> str:
    data = _finnhub_get("/stock/profile2", {"symbol": ticker})
    return (data.get("name") or "") if isinstance(data, dict) else ""


def _find_col(cols, name: str):
    target = name.lower().replace(" ", "")
    for c in cols:
        if str(c).lower().replace(" ", "") == target:
            return c
    return None


def _consensus(tk, ticker: str) -> tuple[float | None, str | None, int]:
    fh = _finnhub_recommendation(ticker)
    if fh is not None:
        return fh
    rec = None
    try:
        _yf_throttle(); rec = tk.get_recommendations()
    except Exception as e:
        log.debug(f"get_recommendations() failed ({e}), trying .recommendations attribute.")
        try: rec = tk.recommendations
        except Exception as e2:
            log.debug(f".recommendations also failed ({e2}).")
            rec = None
    if rec is not None and len(rec):
        cols = list(rec.columns)
        if _find_col(cols, "strongBuy") is not None:
            row = rec.iloc[-1]
            pc  = _find_col(cols, "period")
            if pc is not None and (rec[pc] == "0m").any():
                row = rec[rec[pc] == "0m"].iloc[-1]
            def g(n):
                c = _find_col(cols, n)
                return float(row[c]) if c is not None and pd.notna(row[c]) else 0.0
            sb, b, h, s, ss = g("strongBuy"), g("buy"), g("hold"), g("sell"), g("strongSell")
            n = sb + b + h + s + ss
            if n > 0:
                avg = (5 * sb + 4 * b + 3 * h + 2 * s + 1 * ss) / n
                return round(avg, 2), _rating_label(avg), int(n)
    return None, None, 0


def _recent_actions(tk, now):
    try:
        _yf_throttle(); ud = tk.get_upgrades_downgrades()
    except Exception:
        ud = None
    if ud is None or len(ud) == 0:
        return None, "none", None, 0, None
    ud = ud.reset_index()
    ud.columns = [str(c).lower().replace(" ", "") for c in ud.columns]
    dcol = _find_col(ud.columns, "gradedate") or _find_col(ud.columns, "date") or ud.columns[0]
    try:
        ud[dcol] = pd.to_datetime(ud[dcol], utc=True).dt.tz_convert(None)
    except Exception:
        ud[dcol] = pd.to_datetime(ud[dcol], errors="coerce")
    ud = ud.dropna(subset=[dcol]).sort_values(dcol)
    if ud.empty:
        return None, "none", None, 0, None
    latest        = ud.iloc[-1]
    latest_action = _ACTION_LABEL.get(str(latest.get("action", "")).lower(), str(latest.get("action", "")) or "none")
    latest_firm   = latest.get("firm") if "firm" in ud.columns else None
    days_since    = int((now - latest[dcol]).days)
    cutoff = now - pd.Timedelta(days=SENTIMENT_WINDOW_DAYS)
    recent = ud[ud[dcol] >= cutoff]
    if recent.empty:
        return None, latest_action, (latest_firm or None), 0, days_since
    num = den = 0.0
    for _, r in recent.iterrows():
        days_ago = max(0, (now - r[dcol]).days)
        w        = max(0.1, 1.0 - days_ago / SENTIMENT_WINDOW_DAYS)
        act      = str(r.get("action", "")).lower()
        act_dir  = 1.0 if act == "up" else -1.0 if act == "down" else 0.0
        grade    = _GRADE_MAP.get(str(r.get("tograde", "")).lower())
        row_score = act_dir if grade is None else 0.5 * act_dir + 0.5 * grade
        num += w * row_score
        den += w
    action_score = round(num / den, 3) if den else None
    return action_score, latest_action, (latest_firm or None), int(len(recent)), days_since


def _price_targets(tk):
    try:
        _yf_throttle()
        pt = tk.get_analyst_price_targets()
        if isinstance(pt, dict):
            cur, mean = pt.get("current"), pt.get("mean")
            if cur and mean and float(cur) > 0:
                up = (float(mean) - float(cur)) / float(cur) * 100
                return float(cur), float(mean), round(up, 2)
    except Exception as e:
        log.debug(f"get_analyst_price_targets() failed: {e}")
    return None, None, None


def _company_keywords(ticker: str, name: str):
    pats = []
    t = ticker.strip()
    if len(t) >= 2:
        pats.append(re.compile(r"\b" + re.escape(t) + r"\b"))
    name = name or ""
    for w in re.split(r"[^A-Za-z0-9&]+", name):
        wl = w.strip().lower()
        if len(wl) >= 4 and wl not in _NAME_STOPWORDS:
            pats.append(re.compile(re.escape(wl), re.IGNORECASE))
            break
    return pats


def _filter_relevant(titles: list[str], pats) -> list[str]:
    if not pats:
        return []
    return [t for t in titles if any(p.search(t) for p in pats)]


def _finnhub_raw_headlines(ticker: str):
    if not FINNHUB_API_KEY:
        return None
    try:
        _finnhub_throttle()
        to_d   = datetime.now(timezone.utc).date()
        from_d = to_d - timedelta(days=FINNHUB_NEWS_DAYS)
        resp = requests.get(
            f"{FINNHUB_BASE}/company-news",
            params={"symbol": ticker, "from": from_d.isoformat(), "to": to_d.isoformat(), "token": FINNHUB_API_KEY},
            timeout=15,
        )
        if resp.status_code != 200:
            return None
        data = resp.json()
        if not isinstance(data, list):
            return None
        items  = sorted(data, key=lambda x: x.get("datetime", 0), reverse=True)
        titles, seen = [], set()
        for it in items:
            h = (it.get("headline") or "").strip()
            if h and h.lower() not in seen:
                seen.add(h.lower()); titles.append(h)
            if len(titles) >= FINNHUB_FETCH_MAX:
                break
        return titles
    except Exception as e:
        log.warning(f"{ticker}: Finnhub news failed — {e}")
        return None


def _yfinance_raw_headlines(tk) -> list[str]:
    try:
        _yf_throttle(); news = tk.news
    except Exception:
        return []
    titles = []
    for item in (news or []):
        if not isinstance(item, dict): continue
        title   = None
        content = item.get("content")
        if isinstance(content, dict): title = content.get("title")
        title = title or item.get("title")
        if title and isinstance(title, str): titles.append(title.strip())
    return titles


def _news_titles_for(tk, ticker: str, name: str) -> list[str]:
    raw = _finnhub_raw_headlines(ticker)
    if raw is None:
        raw = _yfinance_raw_headlines(tk)
    relevant = _filter_relevant(raw, _company_keywords(ticker, name))
    if len(relevant) < NEWS_MIN_RELEVANT:
        return []
    return relevant[:NEWS_LIMIT]


# ---- the provider ----

class USMarketDataProvider(MarketDataProvider):
    market = "us"

    def get_universe(self) -> list[str]:
        """Top 100 US large-caps via EquityQuery screener, hardcoded fallback."""
        log.info("Fetching top 100 tickers via EquityQuery screener...")
        rules = self.config.get("universe", {})
        min_cap = int(rules.get("min_market_cap", 10_000_000_000))
        size = int(rules.get("size", 100))
        try:
            from yfinance import EquityQuery, screen

            q = EquityQuery("and", [
                EquityQuery("eq",    ["region", "us"]),
                EquityQuery("is-in", ["exchange", "NMS", "NYQ"]),
                EquityQuery("gt",    ["intradaymarketcap", min_cap]),
            ])
            result = screen(q, sortField="intradaymarketcap", sortAsc=False, size=200)

            raw_tickers = []
            for item in result.get("quotes", []):
                sym = item.get("symbol")
                if not sym or "-" in sym:
                    continue
                if item.get("financialCurrency") not in ("USD", None):
                    continue
                if sym in _FOREIGN_ADR_DENYLIST:
                    continue
                raw_tickers.append(sym)
            raw_tickers = raw_tickers[:size]

            fallback_set = set(_FALLBACK_TICKERS)
            known_us = sum(1 for t in raw_tickers if t in fallback_set)
            overlap_pct = known_us / len(raw_tickers) if raw_tickers else 0

            if len(raw_tickers) >= 80 and overlap_pct >= 0.6:
                log.info(f"Screener: {len(raw_tickers)} tickers ({overlap_pct:.0%} overlap).")
                return raw_tickers
            log.warning(f"Screener suspicious ({len(raw_tickers)} tickers, {overlap_pct:.0%} overlap). Falling back.")
            return _FALLBACK_TICKERS

        except Exception as e:
            log.warning(f"EquityQuery screener failed ({e}), using fallback list.")
            return _FALLBACK_TICKERS

    def get_ohlcv_batch(self, tickers: list[str], period: str | None = None) -> "pd.DataFrame | None":
        fetch = self.config.get("fetch", {})
        try:
            return yf.download(
                tickers=tickers,
                period=period or fetch.get("period", "6mo"),
                interval=fetch.get("interval", "1d"),
                auto_adjust=True,
                progress=False,
                group_by="ticker",
            )
        except Exception as e:
            log.error(f"Batch historical data download failed: {e}")
            return None

    def get_closes(
        self, tickers: list[str], start: str, end: str
    ) -> dict[str, dict[str, float]]:
        try:
            data = yf.download(
                tickers=tickers,
                start=start,
                end=end,
                interval="1d",
                auto_adjust=True,
                progress=False,
                group_by="ticker",
            )
        except Exception as e:
            log.error(f"Closes download failed: {e}")
            return {}
        if data is None or data.empty:
            return {}

        out: dict[str, dict[str, float]] = {}
        is_multi = hasattr(data.columns, "levels")
        for t in tickers:
            try:
                if is_multi:
                    if t not in data.columns.get_level_values(0):
                        continue
                    closes = data[t]["Close"].dropna()
                else:
                    closes = data["Close"].dropna()
                out[t] = {idx.strftime("%Y-%m-%d"): round(float(v), 6) for idx, v in closes.items()}
            except Exception as e:
                log.debug(f"{t}: closes slice failed — {e}")
        return out

    def gather_ticker_context(self, ticker: str) -> dict | None:
        """Network I/O phase (runs in thread pool): analyst data + news headlines."""
        try:
            tk  = yf.Ticker(ticker)
            now = pd.Timestamp(datetime.now(timezone.utc)).tz_localize(None)
            name = _finnhub_profile_name(ticker)
            avg, rating_label, n_analysts = _consensus(tk, ticker)
            action_score, latest_action, latest_firm, win_count, days_since = _recent_actions(tk, now)
            pt_cur, pt_mean, pt_up = _price_targets(tk)
            titles = _news_titles_for(tk, ticker, name)
            return {
                "ticker": ticker, "avg": avg, "rating_label": rating_label, "n_analysts": n_analysts,
                "action_score": action_score, "latest_action": latest_action, "latest_firm": latest_firm,
                "win_count": win_count, "days_since": days_since,
                "pt_cur": pt_cur, "pt_mean": pt_mean, "pt_up": pt_up,
                "titles": titles,
            }
        except Exception as e:
            log.error(f"{ticker}: gather failed — {e}")
            return None
