import React from 'react'
import { useNavigate } from 'react-router-dom'
import { FolderOpen, AlertTriangle } from 'lucide-react'
import { useActiveGoal } from '@/features/goals/useActiveGoal'
import { TargetMix } from '@/features/recommendations/TargetMix'
import './Portfolios.css'

const riskColor = (band) => {
    switch (band?.toLowerCase()) {
        case 'conservative': return 'var(--qw-buy)'
        case 'moderate':     return 'var(--qw-amber)'
        case 'aggressive':   return 'var(--qw-sell)'
        default:             return 'var(--qw-text-dim)'
    }
}

const GOAL_LABELS = {
    Retirement: 'Retirement',
    LongTermWealth: 'Long-term wealth',
    MediumTermGoal: 'Medium-term goal',
    SpeculationLearning: 'Speculation & learning',
}

const ENGAGEMENT_LABELS = {
    SetAndForget: 'Set & forget',
    Monthly: 'Monthly',
    Daily: 'Daily',
}

const fmtDate = (s) => new Date(s).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })

const Portfolios = () => {
    const navigate = useNavigate()
    const { goal, profile, isOnboarded, isLoading, isError, refetch } = useActiveGoal()

    return (
        <div className="portfolios-page">
            <div className="portfolios-body">
                <div className="portfolios-hero">
                    <h1 className="gradient-text">My Profile</h1>
                    <p>Your scored investor profile and target investment mix.</p>
                </div>

                {isLoading ? (
                    <div className="portfolios-loading">
                        <div className="loading-spinner"></div>
                        <p>Loading your goal…</p>
                    </div>
                ) : isError ? (
                    <div className="portfolios-error">
                        <div className="icon"><AlertTriangle size={40} strokeWidth={1.5} aria-hidden="true" /></div>
                        <p>Failed to load your goal.</p>
                        <button onClick={() => refetch()} className="retry-btn">Try Again</button>
                    </div>
                ) : isOnboarded ? (
                    <div className="portfolios-content">
                        {/* Risk profile banner — scored server-side (§ 2.2) */}
                        <div className="portfolio-profile-card" style={{
                            borderTop: `4px solid ${riskColor(profile.riskBand)}`
                        }}>
                            <div className="profile-badge" style={{ color: riskColor(profile.riskBand) }}>
                                {profile.riskBand}
                            </div>
                            <div className="profile-meta">
                                <span>Goal: <strong>{GOAL_LABELS[goal.type] ?? goal.type}</strong></span>
                                <span>Horizon: <strong>{goal.horizonYears} years</strong></span>
                                <span>Capacity: <strong>{profile.capacity}/100</strong></span>
                                <span>Tolerance: <strong>{profile.tolerance}/100</strong></span>
                            </div>
                            <p className="profile-market-reaction">
                                Effective risk is the lower of the two: <em>{profile.effectiveRisk}/100</em> ·
                                updates {ENGAGEMENT_LABELS[profile.engagement] ?? profile.engagement}
                            </p>
                        </div>

                        {/* Target mix — same view as the dashboard */}
                        <div className="portfolio-mix-card">
                            <TargetMix />
                        </div>

                        {/* Metadata */}
                        <div className="portfolio-meta-row">
                            <span>Profile v{profile.version} · scored {fmtDate(profile.createdAt)} (engine {profile.scoringVersion})</span>
                            {goal.updatedAt && <span>Goal updated {fmtDate(goal.updatedAt)}</span>}
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
                        <h3>No goal yet</h3>
                        <p>Complete the onboarding questionnaire to create your investor profile and target allocation.</p>
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
