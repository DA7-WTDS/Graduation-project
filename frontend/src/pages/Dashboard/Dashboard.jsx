import React from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import './Dashboard.css'

// MOCK DATA - Replace with API calls to your backend
const MOCK_DATA = {
    portfolio: {
        totalValue: 127482.50,
        dailyChange: 1247.35,
        dailyChangePercent: 2.4,
        totalReturn: 24482.50,
        totalReturnPercent: 23.7
    },
    quickStats: [
        { label: 'Today\'s Gain', value: '$1,247', change: '+2.4%', positive: true },
        { label: 'Total Return', value: '$24,482', change: '+23.7%', positive: true },
        { label: 'Cash Available', value: '$2,450', change: null, positive: null }
    ],
    allocation: [
        { label: 'Stocks', value: 40, color: 'var(--color-primary-purple)', amount: '$50,993' },
        { label: 'Bonds', value: 35, color: 'var(--color-primary-teal)', amount: '$44,619' },
        { label: 'ETFs', value: 20, color: 'var(--color-primary-navy)', amount: '$25,496' },
        { label: 'Cash', value: 5, color: 'var(--color-gray-300)', amount: '$6,374' }
    ],
    holdings: [
        { symbol: 'AAPL', name: 'Apple Inc.', shares: 50, price: 182.45, value: 9122.50, todayChange: 2.3, totalReturn: 15.2 },
        { symbol: 'MSFT', name: 'Microsoft Corp.', shares: 30, price: 420.12, value: 12603.60, todayChange: 1.8, totalReturn: 22.4 },
        { symbol: 'GOOGL', name: 'Alphabet Inc.', shares: 25, price: 142.85, value: 3571.25, todayChange: -0.5, totalReturn: 8.3 },
        { symbol: 'AMZN', name: 'Amazon.com Inc.', shares: 15, price: 178.32, value: 2674.80, todayChange: 3.1, totalReturn: 31.5 },
        { symbol: 'TSLA', name: 'Tesla Inc.', shares: 20, price: 248.50, value: 4970.00, todayChange: 5.2, totalReturn: -4.7 }
    ],
    recommendations: [
        {
            id: 1,
            type: 'Rebalance Advisory',
            confidence: 'High',
            text: 'Your stock allocation is now 42% vs target 40%. Our AI suggests reducing tech exposure by $2,500 to maintain your target allocation.',
            actions: ['View Details', 'Dismiss']
        },
        {
            id: 2,
            type: 'Buy Opportunity',
            confidence: 'Medium',
            text: 'GOOGL is down 3% today and approaching your target buy price of $140. Consider reviewing this opportunity with your broker.',
            actions: ['View Details', 'Dismiss']
        }
    ],
    activities: [
        { icon: '📊', title: 'Portfolio Analysis', description: 'AI completed your weekly portfolio health check', time: '2 hours ago' },
        { icon: '📈', title: 'AI Recommendation', description: 'New rebalancing suggestion available for review', time: '1 day ago' },
        { icon: '🎯', title: 'Goal Progress', description: 'Retirement goal: 32% complete', time: '2 days ago' },
        { icon: '🔔', title: 'Market Alert', description: 'TSLA volatility spike detected — review your exposure', time: '3 days ago' },
        { icon: '📋', title: 'Report Ready', description: 'Your monthly portfolio report is ready to view', time: '1 week ago' }
    ]
}

const Dashboard = () => {
    const { user } = useAuth()
    const { portfolio, quickStats, allocation, holdings, recommendations, activities } = MOCK_DATA
    const initials = user ? `${user.firstName?.charAt(0) ?? ''}${user.lastName?.charAt(0) ?? ''}` : '?'

    return (
        <div className="dashboard">
            {/* Header */}
            <div className="dashboard-header">
                <div className="dashboard-header-content">
                    <div className="dashboard-logo">
                        <Link to="/" style={{ textDecoration: 'none', color: 'inherit' }}>
                            <span className="gradient-text">SmartInvest</span> AI
                        </Link>
                    </div>

                    <div className="dashboard-user">
                        <div className="dashboard-notifications">
                            🔔
                            <span className="notification-badge"></span>
                        </div>
                        <Link to="/profile" className="dashboard-avatar" title="View Profile">{initials}</Link>
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <div className="dashboard-content">
                <div className="dashboard-grid">
                    {/* Portfolio Overview */}
                    <div className="dashboard-card portfolio-overview">
                        <div className="card-title" style={{ opacity: 0.9, marginBottom: 'var(--space-md)' }}>
                            TOTAL PORTFOLIO VALUE
                        </div>
                        <div className="portfolio-value">
                            ${portfolio.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </div>
                        <div className="portfolio-change">
                            <span className={portfolio.dailyChange >= 0 ? 'change-positive' : 'change-negative'}>
                                {portfolio.dailyChange >= 0 ? '▲' : '▼'} ${Math.abs(portfolio.dailyChange).toLocaleString()}
                                ({portfolio.dailyChangePercent >= 0 ? '+' : ''}{portfolio.dailyChangePercent}%)
                            </span>
                            <span style={{ opacity: 0.8, fontSize: 'var(--font-size-base)' }}>Today</span>
                        </div>
                        <div className="portfolio-actions">
                            <button className="portfolio-action-btn">View Report</button>
                            <button className="portfolio-action-btn">AI Insights</button>
                            <button className="portfolio-action-btn">Set Goals</button>
                        </div>
                    </div>

                    {/* Quick Stats */}
                    {quickStats.map((stat, index) => (
                        <div key={index} className="dashboard-card">
                            <div className="card-title">{stat.label}</div>
                            <div className="stat-value-large">{stat.value}</div>
                            {stat.change && (
                                <div className={`stat-change ${stat.positive ? 'change-positive' : 'change-negative'}`}>
                                    {stat.change}
                                </div>
                            )}
                        </div>
                    ))}

                    {/* Performance Chart */}
                    <div className="dashboard-card chart-container">
                        <div className="card-header">
                            <div className="card-title">Performance</div>
                            <div className="card-action">1Y</div>
                        </div>
                        <div className="chart-placeholder">
                            <div className="chart-line">
                                <svg viewBox="0 0 400 120" preserveAspectRatio="none">
                                    <defs>
                                        <linearGradient id="chartGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                                            <stop offset="0%" stopColor="#6C63FF" />
                                            <stop offset="100%" stopColor="#00C9A7" />
                                        </linearGradient>
                                        <linearGradient id="chartFill" x1="0%" y1="0%" x2="0%" y2="100%">
                                            <stop offset="0%" stopColor="rgba(108, 99, 255, 0.2)" />
                                            <stop offset="100%" stopColor="rgba(108, 99, 255, 0)" />
                                        </linearGradient>
                                    </defs>
                                    <polyline
                                        points="0,80 40,75 80,70 120,60 160,65 200,50 240,55 280,40 320,35 360,25 400,20"
                                        fill="url(#chartFill)"
                                        stroke="url(#chartGradient)"
                                        strokeWidth="3"
                                    />
                                </svg>
                            </div>
                        </div>
                    </div>

                    {/* Asset Allocation */}
                    <div className="dashboard-card">
                        <div className="card-header">
                            <div className="card-title">Asset Allocation</div>
                            <div className="card-action">Details</div>
                        </div>
                        <div className="allocation-visual">
                            <div className="allocation-pie"></div>
                            <div className="allocation-legend">
                                {allocation.map((item, index) => (
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
                    </div>

                    {/* AI Recommendations */}
                    <div className="dashboard-card ai-recommendations">
                        <div className="card-header">
                            <div className="card-title">🤖 AI Recommendations</div>
                            <div className="card-action">View All</div>
                        </div>
                        {recommendations.map((rec) => (
                            <div key={rec.id} className="recommendation-card">
                                <div className="recommendation-header">
                                    <div>
                                        <span className="recommendation-badge">
                                            ⚡ {rec.confidence} Confidence
                                        </span>
                                    </div>
                                </div>
                                <div className="recommendation-text">{rec.text}</div>
                                <div className="recommendation-actions">
                                    {rec.actions.map((action, idx) => (
                                        <button
                                            key={idx}
                                            className={`rec-btn ${idx === 0 ? 'rec-btn-primary' : 'rec-btn-secondary'}`}
                                        >
                                            {action}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Holdings Table */}
                    <div className="dashboard-card holdings-table-container">
                        <div className="card-header">
                            <div className="card-title">Holdings</div>
                            <div className="card-action">View All</div>
                        </div>
                        <table className="holdings-table">
                            <thead>
                                <tr>
                                    <th>Symbol</th>
                                    <th>Shares</th>
                                    <th>Price</th>
                                    <th>Value</th>
                                    <th>Today</th>
                                    <th>Total Return</th>
                                </tr>
                            </thead>
                            <tbody>
                                {holdings.map((holding) => (
                                    <tr key={holding.symbol}>
                                        <td>
                                            <div className="holding-symbol">{holding.symbol}</div>
                                            <div className="holding-name">{holding.name}</div>
                                        </td>
                                        <td>{holding.shares}</td>
                                        <td>${holding.price.toFixed(2)}</td>
                                        <td>${holding.value.toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                                        <td className={holding.todayChange >= 0 ? 'change-positive' : 'change-negative'}>
                                            {holding.todayChange >= 0 ? '+' : ''}{holding.todayChange}%
                                        </td>
                                        <td className={holding.totalReturn >= 0 ? 'change-positive' : 'change-negative'}>
                                            {holding.totalReturn >= 0 ? '+' : ''}{holding.totalReturn}%
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {/* Recent Activity */}
                    <div className="dashboard-card activity-feed">
                        <div className="card-header">
                            <div className="card-title">Recent Activity</div>
                            <div className="card-action">View All</div>
                        </div>
                        {activities.map((activity, index) => (
                            <div key={index} className="activity-item">
                                <div className="activity-icon">{activity.icon}</div>
                                <div className="activity-content">
                                    <div className="activity-title">{activity.title}</div>
                                    <div className="activity-description">{activity.description}</div>
                                    <div className="activity-time">{activity.time}</div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Dashboard
