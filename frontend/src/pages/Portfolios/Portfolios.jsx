import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { FolderOpen, AlertTriangle } from 'lucide-react'
import { getMyPortfolio } from '../../services/portfolioService'
import { TargetMix } from '@/features/recommendations/TargetMix'
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
            if (err.status === 404) {
                setPortfolio(null)
            } else {
                setError('Failed to load portfolio data.')
            }
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="portfolios-page">
            <div className="portfolios-body">
                <div className="portfolios-hero">
                    <h1 className="gradient-text">My Portfolio</h1>
                    <p>Your risk profile and target investment mix.</p>
                </div>

                {loading ? (
                    <div className="portfolios-loading">
                        <div className="loading-spinner"></div>
                        <p>Loading your portfolio…</p>
                    </div>
                ) : error ? (
                    <div className="portfolios-error">
                        <div className="icon"><AlertTriangle size={40} strokeWidth={1.5} aria-hidden="true" /></div>
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

                        {/* Target mix — same view as the dashboard */}
                        <div className="portfolio-mix-card">
                            <TargetMix />
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
                        <div className="icon"><FolderOpen size={48} strokeWidth={1.25} aria-hidden="true" /></div>
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
