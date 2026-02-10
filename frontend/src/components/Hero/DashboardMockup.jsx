import React from 'react'
import './DashboardMockup.css'

const DashboardMockup = () => {
    return (
        <div className="dashboard-mockup">
            <div className="mockup-header">
                <div>
                    <div className="mockup-title">Portfolio Value</div>
                    <div className="mockup-value">
                        $127,482.50
                        <span className="mockup-change">+12.4%</span>
                    </div>
                </div>
            </div>

            <div className="mockup-stats">
                <div className="mockup-stat">
                    <div className="mockup-stat-label">Today's Gain</div>
                    <div className="mockup-stat-value" style={{ color: 'var(--color-success)' }}>+$1,247</div>
                </div>
                <div className="mockup-stat">
                    <div className="mockup-stat-label">Total Return</div>
                    <div className="mockup-stat-value">+$24,482</div>
                </div>
                <div className="mockup-stat">
                    <div className="mockup-stat-label">Risk Score</div>
                    <div className="mockup-stat-value">Medium</div>
                </div>
            </div>

            <div className="mockup-chart">
                <div className="chart-line">
                    <svg viewBox="0 0 300 100" preserveAspectRatio="none">
                        <polyline
                            points="0,80 30,70 60,75 90,55 120,60 150,45 180,50 210,30 240,35 270,20 300,15"
                            fill="none"
                            stroke="url(#gradient)"
                            strokeWidth="2"
                        />
                        <defs>
                            <linearGradient id="gradient" x1="0%" y1="0%" x2="100%" y2="0%">
                                <stop offset="0%" stopColor="#6C63FF" />
                                <stop offset="100%" stopColor="#00C9A7" />
                            </linearGradient>
                        </defs>
                    </svg>
                </div>
            </div>

            <div className="mockup-portfolio">
                <div className="portfolio-pie"></div>
                <div className="portfolio-legend">
                    <div className="legend-item">
                        <span className="legend-dot" style={{ background: 'var(--color-primary-purple)' }}></span>
                        <span className="legend-label">Stocks</span>
                        <span className="legend-value">40%</span>
                    </div>
                    <div className="legend-item">
                        <span className="legend-dot" style={{ background: 'var(--color-primary-teal)' }}></span>
                        <span className="legend-label">Bonds</span>
                        <span className="legend-value">35%</span>
                    </div>
                    <div className="legend-item">
                        <span className="legend-dot" style={{ background: 'var(--color-primary-navy)' }}></span>
                        <span className="legend-label">ETFs</span>
                        <span className="legend-value">20%</span>
                    </div>
                    <div className="legend-item">
                        <span className="legend-dot" style={{ background: 'var(--color-gray-300)' }}></span>
                        <span className="legend-label">Cash</span>
                        <span className="legend-value">5%</span>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default DashboardMockup
