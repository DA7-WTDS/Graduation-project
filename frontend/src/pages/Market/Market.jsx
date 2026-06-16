import React, { useState } from 'react'
import { Search } from 'lucide-react'
import { useDebounced, useSymbolSearch, useQuote } from '@/features/market/useMarket'
import { LoadingState, EmptyState, ErrorState } from '@/shared/ui'
import './Market.css'

// Default watchlist shown when the search box is empty.
const WATCHLIST = [
    { symbol: 'AAPL', description: 'Apple Inc.' },
    { symbol: 'MSFT', description: 'Microsoft Corp.' },
    { symbol: 'GOOGL', description: 'Alphabet Inc.' },
    { symbol: 'AMZN', description: 'Amazon.com Inc.' },
    { symbol: 'NVDA', description: 'NVIDIA Corp.' },
    { symbol: 'TSLA', description: 'Tesla Inc.' },
    { symbol: 'META', description: 'Meta Platforms Inc.' },
    { symbol: 'SPY', description: 'SPDR S&P 500 ETF' },
]

const fmtUSD = (n) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n)

const AssetCard = ({ symbol, description }) => {
    const { data: quote, isLoading, isError } = useQuote(symbol)
    const up = quote ? quote.change >= 0 : true

    return (
        <div className="asset-card">
            <div className="asset-header">
                <div className="asset-info">
                    <span className="asset-symbol">{symbol}</span>
                    <span className="asset-name">{description}</span>
                </div>
            </div>

            {isLoading ? (
                <div className="asset-quote-muted">Loading quote…</div>
            ) : isError || !quote ? (
                <div className="asset-quote-muted">Quote unavailable</div>
            ) : (
                <>
                    <div className="asset-price-section">
                        <div className="current-price">{fmtUSD(quote.current)}</div>
                        <div className={`price-change ${up ? 'positive' : 'negative'}`}>
                            {up ? '▲' : '▼'} {fmtUSD(Math.abs(quote.change))} ({quote.percentChange > 0 ? '+' : ''}{quote.percentChange.toFixed(2)}%)
                        </div>
                    </div>

                    <div className="asset-stats">
                        <div><span>Open</span><b>{fmtUSD(quote.open)}</b></div>
                        <div><span>High</span><b>{fmtUSD(quote.high)}</b></div>
                        <div><span>Low</span><b>{fmtUSD(quote.low)}</b></div>
                        <div><span>Prev</span><b>{fmtUSD(quote.previousClose)}</b></div>
                    </div>
                </>
            )}
        </div>
    )
}

const Market = () => {
    const [searchTerm, setSearchTerm] = useState('')
    const debounced = useDebounced(searchTerm.trim())
    const isSearching = debounced.length >= 1
    const search = useSymbolSearch(debounced)

    const assets = isSearching
        ? (search.data ?? []).slice(0, 12).map((r) => ({ symbol: r.symbol, description: r.description }))
        : WATCHLIST

    return (
        <div className="market-page">
            <div className="market-body">
                <div className="market-hero">
                    <span className="demo-badge">Live · Finnhub</span>
                    <h1 className="gradient-text">Market Hub</h1>
                    <p>Search any ticker or company for a live quote.</p>
                </div>

                <div className="market-controls">
                    <div className="search-bar">
                        <span className="search-icon"><Search size={18} aria-hidden="true" /></span>
                        <input
                            type="text"
                            placeholder="Search ticker or company (e.g. AAPL, Tesla)…"
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                        />
                    </div>
                </div>

                {isSearching && search.isLoading ? (
                    <LoadingState label="Searching…" />
                ) : isSearching && search.isError ? (
                    <ErrorState message="Search failed. Please try again." onRetry={() => search.refetch()} />
                ) : isSearching && assets.length === 0 ? (
                    <EmptyState title="No matches" hint={`No symbols found for “${debounced}”.`} />
                ) : (
                    <>
                        <div className="market-section-label">
                            {isSearching ? `Results for “${debounced}”` : 'Watchlist'}
                        </div>
                        <div className="market-grid">
                            {assets.map((a) => (
                                <AssetCard key={a.symbol} symbol={a.symbol} description={a.description} />
                            ))}
                        </div>
                    </>
                )}
            </div>
        </div>
    )
}

export default Market
