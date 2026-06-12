import React, { useState, useEffect } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { getMyPortfolio } from '../../services/portfolioService'
import './Portfolios.css'

const riskColor = (profile) => {
    switch (profile?.toLowerCase()) {
        case 'conservative': return 'var(--color-primary-teal)'
        case 'moderate':     return '#f59e0b'
        case 'aggressive':   return '#ef4444'
        default:             return 'var(--color-primary-purple)'
    }
}

const Portfolios = () => {
    const navigate = useNavigate()
    const [portfolio, setPortfolio] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    useEffect(() => {
        fetchPortfolio()
    }, [])

    const fetchPortfolio = async () => {
        setLoading(true)
        setError(null)
        try {
            const data = await getMyPortfolio()
            setPortfolio(data)
        } catch (err) {
            if (err.message?.includes('404') || err.message?.includes('not found')) {
                setPortfolio(null)
            } else {
                setError('Failed to load portfolio data.')
            }
        } finally {
            setLoading(false)
        }
    }

    const allocation = portfolio ? [
        { label: 'Stocks', value: portfolio.stocksPercentage, color: 'var(--color-primary-purple)' },
        { label: 'Bonds',  value: portfolio.bondsPercentage,  color: 'var(--color-primary-teal)' },
        { label: 'ETFs',   value: portfolio.etfsPercentage,   color: '#f59e0b' },
        { label: 'Cash',   value: portfolio.cashPercentage,   color: 'var(--color-gray-300)' },
    ] : []

    return (
        <div className="portfolios-page">
            <header className="page-header">
                <div className="header-content">
                    <Link to="/dashboard" className="back-link">← Dashboard</Link>
                    <div className="header-logo"><span className="gradient-text">SmartInvest</span> AI</div>
                    <div className="header-actions">
                        <span className="user-badge">Management</span>
                    </div>
                </div>
            </header>

            <div className="portfolios-body">
                <div className="portfolios-hero">
                    <h1 className="gradient-text">My Portfolio</h1>
                    <p>Your risk profile and target asset allocation from onboarding.</p>
                </div>

                {loading ? (
                    <div className="portfolios-loading">
                        <div className="loading-spinner"></div>
                        <p>Loading your portfolio…</p>
                    </div>
                ) : error ? (
                    <div className="portfolios-error">
                        <div className="icon">⚠️</div>
                        <p>{error}</p>
                        <button onClick={fetchPortfolio} className="retry-btn">Try Again</button>
                    </div>
                ) : portfolio ? (
                    <div className="portfolios-content">
                        {/* Risk profile banner */}
                        <div className="portfolio-profile-card" style={{
                            borderTop: `4px solid ${riskColor(portfolio.riskProfile)}`
                        }}>
                            <div className="profile-badge" style={{ color: riskColor(portfolio.riskProfile) }}>
                                {portfolio.riskProfile}
                            </div>
                            <div className="profile-meta">
                                <span>Goal: <strong>{portfolio.primaryGoal}</strong></span>
                                <span>Horizon: <strong>{portfolio.timeHorizon}</strong></span>
                                <span>Experience: <strong>{portfolio.investmentExperience}</strong></span>
                                <span>Risk Tolerance: <strong>{portfolio.riskTolerance}%</strong></span>
                            </div>
                            <p className="profile-market-reaction">
                                Market dip reaction: <em>{portfolio.marketReaction}</em>
                            </p>
                        </div>

                        {/* Allocation grid */}
                        <div className="allocation-grid">
                            {allocation.map((item) => (
                                <div key={item.label} className="allocation-card">
                                    <div className="alloc-bar-wrap">
                                        <div
                                            className="alloc-bar-fill"
                                            style={{
                                                height: `${item.value}%`,
                                                background: item.color,
                                                minHeight: item.value > 0 ? '6px' : 0
                                            }}
                                        />
                                    </div>
                                    <div className="alloc-value">{item.value}%</div>
                                    <div className="alloc-label">{item.label}</div>
                                </div>
                            ))}
                        </div>

                        {/* Metadata */}
                        <div className="portfolio-meta-row">
                            <span>Created {new Date(portfolio.createdAt).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}</span>
                            {portfolio.updatedAt && (
                                <span>Last updated {new Date(portfolio.updatedAt).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}</span>
                            )}
                            <button
                                className="edit-profile-btn"
                                onClick={() => navigate('/onboarding')}
                            >
                                Retake Questionnaire
                            </button>
                        </div>
                    </div>
                ) : (
                    <div className="portfolios-placeholder">
                        <div className="icon">📁</div>
                        <h3>No portfolio yet</h3>
                        <p>Complete the onboarding questionnaire to create your risk profile and target allocation.</p>
                        <button
                            className="start-onboarding-btn"
                            onClick={() => navigate('/onboarding')}
                        >
                            Start Onboarding
                        </button>
                    </div>
                )}
            </div>
        </div>
    )
}

export default Portfolios
