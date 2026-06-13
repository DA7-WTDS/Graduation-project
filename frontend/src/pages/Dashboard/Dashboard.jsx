import React from 'react'
import { usePortfolio } from '@/features/portfolio/usePortfolio'
import { useNotifications, formatRelativeTime } from '@/features/notifications/useNotifications'
import { RecommendationsPanel } from '@/features/recommendations/RecommendationsPanel'
import './Dashboard.css'

const Dashboard = () => {
    const { data: portfolio, isLoading: portfolioLoading, isError: portfolioError } = usePortfolio()
    const { notifications, markAsRead } = useNotifications()

    // Build allocation array from portfolio data
    const allocationData = portfolio ? [
        { label: 'Stocks',  value: portfolio.stocksPercentage,  color: 'var(--qw-amber)' },
        { label: 'Bonds',   value: portfolio.bondsPercentage,   color: 'var(--qw-amber-dim)' },
        { label: 'ETFs',    value: portfolio.etfsPercentage,    color: 'var(--qw-text-dim)' },
        { label: 'Cash',    value: portfolio.cashPercentage,    color: 'var(--qw-text-faint)' },
    ].filter(a => a.value > 0) : []

    return (
        <div className="dashboard">
            {/* Main Content */}
            <div className="dashboard-content">
                <div className="dashboard-grid">

                    {/* Portfolio Overview */}
                    <div className="dashboard-card portfolio-overview">
                        {portfolioLoading ? (
                            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', opacity: 0.7 }}>
                                <div className="loading-spinner" style={{ width: 20, height: 20 }}></div>
                                <span>Loading portfolio…</span>
                            </div>
                        ) : portfolioError ? (
                            <div style={{ color: 'var(--qw-sell)' }}>Failed to load portfolio.</div>
                        ) : portfolio ? (
                            <>
                                <div className="card-title" style={{ opacity: 0.9, marginBottom: 'var(--space-md)' }}>
                                    RISK PROFILE
                                </div>
                                <div className="portfolio-value" style={{ fontSize: '2rem' }}>
                                    {portfolio.riskProfile}
                                </div>
                                <div style={{ marginTop: 'var(--space-sm)', opacity: 0.75, fontSize: '0.9rem' }}>
                                    {portfolio.primaryGoal} · {portfolio.timeHorizon}
                                </div>
                                <div className="portfolio-actions" style={{ marginTop: 'var(--space-md)' }}>
                                    <button className="portfolio-action-btn" onClick={() => window.location.href = '/portfolios'}>Edit Profile</button>
                                </div>
                            </>
                        ) : (
                            <>
                                <div className="card-title" style={{ opacity: 0.9, marginBottom: 'var(--space-md)' }}>
                                    NO PORTFOLIO YET
                                </div>
                                <div style={{ opacity: 0.7, marginBottom: 'var(--space-md)' }}>
                                    Complete the onboarding questionnaire to get started.
                                </div>
                                <div className="portfolio-actions">
                                    <button className="portfolio-action-btn" onClick={() => window.location.href = '/onboarding'}>
                                        Start Onboarding
                                    </button>
                                </div>
                            </>
                        )}
                    </div>

                    {/* Quick Stats — derived from portfolio */}
                    {portfolio && (
                        <>
                            <div className="dashboard-card">
                                <div className="card-title">Risk Tolerance</div>
                                <div className="stat-value-large">{portfolio.riskTolerance}%</div>
                            </div>
                            <div className="dashboard-card">
                                <div className="card-title">Time Horizon</div>
                                <div className="stat-value-large" style={{ fontSize: '1.4rem' }}>{portfolio.timeHorizon}</div>
                            </div>
                            <div className="dashboard-card">
                                <div className="card-title">Experience</div>
                                <div className="stat-value-large" style={{ fontSize: '1.2rem' }}>{portfolio.investmentExperience}</div>
                            </div>
                        </>
                    )}

                    {/* Asset Allocation */}
                    <div className="dashboard-card">
                        <div className="card-header">
                            <div className="card-title">Asset Allocation</div>
                            <div className="card-action">Target Mix</div>
                        </div>
                        {portfolioLoading ? (
                            <div style={{ opacity: 0.6 }}>Loading…</div>
                        ) : allocationData.length > 0 ? (
                            <div className="allocation-visual">
                                <div className="allocation-pie"></div>
                                <div className="allocation-legend">
                                    {allocationData.map((item, index) => (
                                        <div key={index} className="legend-item">
                                            <div className="legend-label">
                                                <span className="legend-dot" style={{ background: item.color }}></span>
                                                {item.label}
                                            </div>
                                            <div className="legend-value">{item.value}%</div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        ) : (
                            <div style={{ opacity: 0.6, fontSize: '0.9rem' }}>
                                Complete onboarding to see your target allocation.
                            </div>
                        )}
                    </div>

                    {/* AI Recommendations — the live product */}
                    <div className="dashboard-card ai-recommendations">
                        <RecommendationsPanel />
                    </div>

                    {/* Recent Activity — from real notifications */}
                    <div className="dashboard-card activity-feed">
                        <div className="card-header">
                            <div className="card-title">Recent Activity</div>
                            <div className="card-action">View All</div>
                        </div>
                        {notifications.length > 0 ? (
                            notifications.slice(0, 5).map((n, index) => (
                                <div
                                    key={n.id || index}
                                    className="activity-item"
                                    style={{ cursor: !n.isRead ? 'pointer' : 'default' }}
                                    onClick={() => !n.isRead && markAsRead(n.id)}
                                >
                                    <div className="activity-icon">
                                        {n.type === 'Recommendation' ? '🤖'
                                            : n.type === 'Alert' ? '🔔'
                                            : n.type === 'System' ? '⚙️'
                                            : '📋'}
                                    </div>
                                    <div className="activity-content">
                                        <div className="activity-title" style={{ fontWeight: n.isRead ? 400 : 600 }}>
                                            {n.title}
                                        </div>
                                        <div className="activity-description">{n.message}</div>
                                        <div className="activity-time">{formatRelativeTime(n.createdAt)}</div>
                                    </div>
                                </div>
                            ))
                        ) : (
                            <div style={{ opacity: 0.55, fontSize: '0.9rem' }}>No recent activity.</div>
                        )}
                    </div>

                </div>
            </div>
        </div>
    )
}

export default Dashboard
