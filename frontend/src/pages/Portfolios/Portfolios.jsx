import React from 'react'
import { useNavigate } from 'react-router-dom'
import { FolderOpen, AlertTriangle } from 'lucide-react'
import { usePortfolio } from '@/features/portfolio/usePortfolio'
import { TargetMix } from '@/features/recommendations/TargetMix'
import './Portfolios.css'

const riskColor = (profile) => {
    switch (profile?.toLowerCase()) {
        case 'conservative': return 'var(--qw-buy)'
        case 'moderate':     return 'var(--qw-amber)'
        case 'aggressive':   return 'var(--qw-sell)'
        default:             return 'var(--qw-text-dim)'
    }
}

const Portfolios = () => {
    const navigate = useNavigate()
    // 404 ("no portfolio yet") resolves to null; isError is only a real failure.
    const { data: portfolio, isLoading, isError, refetch } = usePortfolio()

    return (
        <div className="portfolios-page">
            <div className="portfolios-body">
                <div className="portfolios-hero">
                    <h1 className="gradient-text">My Portfolio</h1>
                    <p>Your risk profile and target investment mix.</p>
                </div>

                {isLoading ? (
                    <div className="portfolios-loading">
                        <div className="loading-spinner"></div>
                        <p>Loading your portfolio…</p>
                    </div>
                ) : isError ? (
                    <div className="portfolios-error">
                        <div className="icon"><AlertTriangle size={40} strokeWidth={1.5} aria-hidden="true" /></div>
                        <p>Failed to load portfolio data.</p>
                        <button onClick={() => refetch()} className="retry-btn">Try Again</button>
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
