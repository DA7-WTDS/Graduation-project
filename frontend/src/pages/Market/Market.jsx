import React, { useState } from 'react'
import { AVAILABLE_ASSETS } from '../../utils/mockHistoricalData'
import './Market.css'

const Market = () => {
    const [searchTerm, setSearchTerm] = useState('')
    const [filter, setFilter] = useState('All')

    const filteredAssets = AVAILABLE_ASSETS.filter(asset => {
        const matchesSearch = asset.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
            asset.symbol.toLowerCase().includes(searchTerm.toLowerCase())
        const matchesFilter = filter === 'All' || asset.type === filter
        return matchesSearch && matchesFilter
    })

    return (
        <div className="market-page">
            <div className="market-body">
                <div className="market-hero">
                    <span className="demo-badge">Demo · mock data</span>
                    <h1 className="gradient-text">Market Hub</h1>
                    <p>Track real-time performance and discover institutional-grade opportunities.</p>
                </div>

                <div className="market-controls">
                    <div className="search-bar">
                        <span className="search-icon">🔍</span>
                        <input
                            type="text"
                            placeholder="Search assets, symbols, or sectors..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                        />
                    </div>
                    <div className="filter-chips">
                        {['All', 'Stock', 'Crypto', 'ETF'].map(t => (
                            <button
                                key={t}
                                className={`chip ${filter === t ? 'active' : ''}`}
                                onClick={() => setFilter(t)}
                            >
                                {t}
                            </button>
                        ))}
                    </div>
                </div>

                <div className="market-grid">
                    {filteredAssets.map(asset => (
                        <div key={asset.symbol} className="asset-card">
                            <div className="asset-header">
                                <div className="asset-info">
                                    <span className="asset-symbol">{asset.symbol}</span>
                                    <span className="asset-name">{asset.name}</span>
                                </div>
                                <div className="asset-type-tag">{asset.type}</div>
                            </div>

                            <div className="asset-price-section">
                                <div className="current-price">${asset.basePrice.toLocaleString()}</div>
                                <div className="price-change-mock positive">+2.45%</div>
                            </div>

                            <div className="asset-mini-chart">
                                <svg viewBox="0 0 100 40" preserveAspectRatio="none">
                                    <path
                                        d="M0,35 L10,30 L20,32 L30,25 L40,28 L50,15 L60,18 L70,10 L80,12 L90,5 L100,8"
                                        fill="none"
                                        stroke="var(--color-primary-teal)"
                                        strokeWidth="2"
                                    />
                                </svg>
                            </div>

                            <div className="asset-actions">
                                <button className="primary-btn-sm" style={{ gridColumn: 'span 2' }}>Detailed Analysis</button>
                            </div>
                        </div>
                    ))}
                </div>

                <div className="trending-section">
                    <h3>Trending Insights</h3>
                    <div className="insight-grid">
                        <div className="insight-card">
                            <div className="insight-icon">🔥</div>
                            <div className="insight-content">
                                <h4>Tech Momentum</h4>
                                <p>SaaS sector seeing 15% increase in capital allocation this week.</p>
                            </div>
                        </div>
                        <div className="insight-card">
                            <div className="insight-icon">💡</div>
                            <div className="insight-content">
                                <h4>AI Alpha</h4>
                                <p>Our AI suggests AAPL is 12% undervalued relative to peer multiples.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Market
