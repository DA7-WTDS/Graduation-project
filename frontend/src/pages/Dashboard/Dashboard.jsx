import React, { useState, useEffect, useRef } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { notificationService } from '../../services/notificationService'
import { getMyPortfolio } from '../../services/portfolioService'
import { getRecommendations } from '../../services/recommendationService'
import './Dashboard.css'

const Dashboard = () => {
    const { user } = useAuth()

    // Notifications state
    const [notifications, setNotifications] = useState([])
    const [unreadCount, setUnreadCount] = useState(0)
    const [showNotifications, setShowNotifications] = useState(false)
    const notificationRef = useRef(null)

    // Portfolio state
    const [portfolio, setPortfolio] = useState(null)
    const [portfolioLoading, setPortfolioLoading] = useState(true)
    const [portfolioError, setPortfolioError] = useState(null)

    // Recommendations state
    const [recommendations, setRecommendations] = useState(null)
    const [recsLoading, setRecsLoading] = useState(true)
    const [recsError, setRecsError] = useState(null)

    const initials = user ? `${user.firstName?.charAt(0) ?? ''}${user.lastName?.charAt(0) ?? ''}` : '?'

    useEffect(() => {
        // Expose test function to window for console debugging
        window.triggerTestNotification = async () => {
            try {
                await notificationService.createTestNotification()
                console.log('Test notification triggered!')
                fetchNotifications()
            } catch (err) {
                console.error('Failed to trigger test notification:', err)
            }
        }

        if (user) {
            fetchNotifications()
            fetchPortfolio()
            fetchRecommendations()
        }

        const handleClickOutside = (event) => {
            if (notificationRef.current && !notificationRef.current.contains(event.target)) {
                setShowNotifications(false)
            }
        }
        document.addEventListener('mousedown', handleClickOutside)
        return () => document.removeEventListener('mousedown', handleClickOutside)
    }, [user])

    const fetchNotifications = async () => {
        try {
            const [notifsResponse, countResponse] = await Promise.all([
                notificationService.getNotifications(1, 10),
                notificationService.getUnreadCount()
            ])
            if (notifsResponse && Array.isArray(notifsResponse)) setNotifications(notifsResponse)
            if (typeof countResponse === 'number') setUnreadCount(countResponse)
        } catch (error) {
            console.error('Error fetching notifications:', error)
        }
    }

    const fetchPortfolio = async () => {
        setPortfolioLoading(true)
        setPortfolioError(null)
        try {
            const data = await getMyPortfolio()
            setPortfolio(data)
        } catch (err) {
            // 404 means no portfolio yet (user hasn't completed onboarding)
            if (err.message?.includes('404') || err.message?.includes('not found')) {
                setPortfolio(null)
            } else {
                setPortfolioError('Failed to load portfolio')
            }
        } finally {
            setPortfolioLoading(false)
        }
    }

    const fetchRecommendations = async () => {
        setRecsLoading(true)
        setRecsError(null)
        try {
            const data = await getRecommendations()
            setRecommendations(data)
        } catch (err) {
            // 404 = no run yet; other errors = real error
            if (err.message?.includes('404') || err.message?.includes('not found')) {
                setRecommendations(null)
            } else {
                setRecsError('Recommendations unavailable')
            }
        } finally {
            setRecsLoading(false)
        }
    }

    const handleMarkAsRead = async (id) => {
        try {
            await notificationService.markAsRead(id)
            setNotifications(notifications.map(n => n.id === id ? { ...n, isRead: true } : n))
            setUnreadCount(prev => Math.max(0, prev - 1))
        } catch (error) {
            console.error('Error marking notification as read:', error)
        }
    }

    const handleMarkAllAsRead = async () => {
        try {
            await notificationService.markAllAsRead()
            setNotifications(notifications.map(n => ({ ...n, isRead: true })))
            setUnreadCount(0)
        } catch (error) {
            console.error('Error marking all as read:', error)
        }
    }

    const formatDate = (dateString) => {
        const date = new Date(dateString)
        const now = new Date()
        const diffInSeconds = Math.floor((now - date) / 1000)
        if (diffInSeconds < 60) return 'Just now'
        if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`
        if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`
        return date.toLocaleDateString()
    }

    // Build allocation array from portfolio data
    const allocationData = portfolio ? [
        { label: 'Stocks',  value: portfolio.stocksPercentage,  color: 'var(--color-primary-purple)' },
        { label: 'Bonds',   value: portfolio.bondsPercentage,   color: 'var(--color-primary-teal)' },
        { label: 'ETFs',    value: portfolio.etfsPercentage,    color: 'var(--color-primary-navy)' },
        { label: 'Cash',    value: portfolio.cashPercentage,    color: 'var(--color-gray-300)' },
    ].filter(a => a.value > 0) : []

    // Action label colours
    const actionColor = (action) => {
        switch (action?.toUpperCase()) {
            case 'BUY':     return 'var(--color-primary-teal)'
            case 'SELL':    return '#ef4444'
            case 'HOLD':    return 'var(--color-gray-300)'
            default:        return 'var(--color-primary-purple)'
        }
    }

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

                    <nav className="dashboard-nav">
                        <NavLink to="/dashboard" className={({ isActive }) => `dashboard-nav-link${isActive ? ' active' : ''}`}>
                            Dashboard
                        </NavLink>
                        <NavLink to="/portfolios" className={({ isActive }) => `dashboard-nav-link${isActive ? ' active' : ''}`}>
                            Portfolios
                        </NavLink>
                        <NavLink to="/simulator" className={({ isActive }) => `dashboard-nav-link${isActive ? ' active' : ''}`}>
                            Learning
                        </NavLink>
                        <NavLink to="/market" className={({ isActive }) => `dashboard-nav-link${isActive ? ' active' : ''}`}>
                            Market
                        </NavLink>
                    </nav>

                    <div className="dashboard-user">
                        <div className="dashboard-notifications-wrapper" ref={notificationRef}>
                            <div
                                className="dashboard-notifications"
                                onClick={() => setShowNotifications(!showNotifications)}
                            >
                                🔔
                                {unreadCount > 0 && (
                                    <span className="notification-badge">{unreadCount}</span>
                                )}
                            </div>

                            {showNotifications && (
                                <div className="notifications-dropdown">
                                    <div className="notifications-header">
                                        <h3>Notifications</h3>
                                        {unreadCount > 0 && (
                                            <button onClick={handleMarkAllAsRead}>Mark all as read</button>
                                        )}
                                    </div>
                                    <div className="notifications-list">
                                        {notifications.length > 0 ? (
                                            notifications.map(notification => (
                                                <div
                                                    key={notification.id}
                                                    className={`notification-item ${!notification.isRead ? 'unread' : ''}`}
                                                    onClick={() => !notification.isRead && handleMarkAsRead(notification.id)}
                                                >
                                                    <div className="notification-dot"></div>
                                                    <div className="notification-content">
                                                        <div className="notification-title">{notification.title}</div>
                                                        <div className="notification-message">{notification.message}</div>
                                                        <div className="notification-time">{formatDate(notification.createdAt)}</div>
                                                    </div>
                                                </div>
                                            ))
                                        ) : (
                                            <div className="notifications-empty">No notifications yet</div>
                                        )}
                                    </div>
                                    <div className="notifications-footer">
                                        <button>View all activity</button>
                                    </div>
                                </div>
                            )}
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
                        {portfolioLoading ? (
                            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', opacity: 0.7 }}>
                                <div className="loading-spinner" style={{ width: 20, height: 20 }}></div>
                                <span>Loading portfolio…</span>
                            </div>
                        ) : portfolioError ? (
                            <div style={{ color: '#ef4444' }}>{portfolioError}</div>
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
                                    <button className="portfolio-action-btn" onClick={fetchRecommendations}>Refresh AI</button>
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

                    {/* AI Recommendations */}
                    <div className="dashboard-card ai-recommendations">
                        <div className="card-header">
                            <div className="card-title">🤖 AI Recommendations</div>
                            <div className="card-action" onClick={fetchRecommendations} style={{ cursor: 'pointer' }}>Refresh</div>
                        </div>

                        {recsLoading ? (
                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', opacity: 0.7 }}>
                                <div className="loading-spinner" style={{ width: 18, height: 18 }}></div>
                                <span>Generating recommendations…</span>
                            </div>
                        ) : recsError ? (
                            <div style={{ color: '#ef4444', fontSize: '0.9rem' }}>{recsError}</div>
                        ) : recommendations ? (
                            <>
                                {recommendations.summary && (
                                    <div style={{
                                        background: 'rgba(108,99,255,0.1)',
                                        border: '1px solid rgba(108,99,255,0.2)',
                                        borderRadius: '8px',
                                        padding: '12px 14px',
                                        fontSize: '0.875rem',
                                        lineHeight: 1.6,
                                        marginBottom: '16px',
                                        opacity: 0.9
                                    }}>
                                        {recommendations.summary}
                                    </div>
                                )}
                                {recommendations.picks?.map((pick, idx) => (
                                    <div key={idx} className="recommendation-card">
                                        <div className="recommendation-header">
                                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                                                <span style={{
                                                    fontWeight: 700,
                                                    fontSize: '1rem',
                                                    color: 'var(--color-primary-purple)'
                                                }}>
                                                    {pick.ticker}
                                                </span>
                                                <span className="recommendation-badge" style={{
                                                    background: `${actionColor(pick.action)}22`,
                                                    color: actionColor(pick.action),
                                                    border: `1px solid ${actionColor(pick.action)}44`
                                                }}>
                                                    {pick.action}
                                                </span>
                                                {pick.allocation_pct > 0 && (
                                                    <span style={{ opacity: 0.6, fontSize: '0.8rem' }}>
                                                        {pick.allocation_pct}% allocation
                                                    </span>
                                                )}
                                            </div>
                                        </div>
                                        <div className="recommendation-text">{pick.reason}</div>
                                        {pick.risk_note && (
                                            <div style={{
                                                fontSize: '0.8rem',
                                                opacity: 0.65,
                                                marginTop: '6px',
                                                fontStyle: 'italic'
                                            }}>
                                                ⚠ {pick.risk_note}
                                            </div>
                                        )}
                                    </div>
                                ))}
                                <div style={{ fontSize: '0.75rem', opacity: 0.45, marginTop: '12px' }}>
                                    Generated {new Date(recommendations.generated_at).toLocaleString()}
                                </div>
                            </>
                        ) : (
                            <div style={{ opacity: 0.6, fontSize: '0.9rem' }}>
                                No recommendations available yet. The pipeline runs daily — check back later.
                            </div>
                        )}
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
                                    onClick={() => !n.isRead && handleMarkAsRead(n.id)}
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
                                        <div className="activity-time">{formatDate(n.createdAt)}</div>
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
