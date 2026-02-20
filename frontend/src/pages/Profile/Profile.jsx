import React, { useEffect, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { getUserProfile } from '../../services/authService'
import './Profile.css'

const Profile = () => {
    const navigate = useNavigate()
    const { user, logout, loading: authLoading } = useAuth()
    const [profileData, setProfileData] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')

    useEffect(() => {
        fetchProfile()
    }, [])

    const fetchProfile = async () => {
        try {
            const data = await getUserProfile()
            setProfileData(data)
        } catch (err) {
            console.error('Failed to fetch profile:', err)
            setError('Failed to load profile data')
        } finally {
            setLoading(false)
        }
    }

    const handleLogout = () => {
        logout()
        navigate('/')
    }

    if (authLoading || loading) {
        return (
            <div className="profile-page">
                <div className="profile-orb profile-orb-1"></div>
                <div className="profile-orb profile-orb-2"></div>
                <div className="profile-loading">
                    <div className="loading-spinner"></div>
                    <p>Loading your profile...</p>
                </div>
            </div>
        )
    }

    if (error) {
        return (
            <div className="profile-page">
                <div className="profile-orb profile-orb-1"></div>
                <div className="profile-orb profile-orb-2"></div>
                <div className="profile-error-state">
                    <div className="error-icon">⚠️</div>
                    <p>{error}</p>
                    <button onClick={fetchProfile} className="retry-button">Try Again</button>
                </div>
            </div>
        )
    }

    const displayUser = profileData || user
    const initials = `${displayUser?.firstName?.charAt(0) ?? ''}${displayUser?.lastName?.charAt(0) ?? ''}`
    const roleBadgeClass = displayUser?.role?.toLowerCase() === 'admin' ? 'role-badge role-admin' : 'role-badge role-user'
    const memberSince = displayUser?.createdAt
        ? new Date(displayUser.createdAt).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
        : '—'

    return (
        <div className="profile-page">
            {/* Background decorations */}
            <div className="profile-orb profile-orb-1"></div>
            <div className="profile-orb profile-orb-2"></div>

            {/* Top nav bar */}
            <nav className="profile-nav">
                <Link to="/dashboard" className="profile-nav-back">
                    <span className="back-arrow">←</span>
                    Back to Dashboard
                </Link>
                <div className="profile-nav-logo">
                    <span className="gradient-text">SmartInvest</span> AI
                </div>
            </nav>

            <div className="profile-container">
                {/* Hero card */}
                <div className="profile-hero-card">
                    <div className="profile-hero-bg"></div>
                    <div className="profile-hero-content">
                        <div className="profile-avatar-ring">
                            <div className="profile-avatar-inner">
                                <span className="avatar-initials">{initials}</span>
                            </div>
                        </div>
                        <div className="profile-hero-info">
                            <h1 className="profile-full-name">
                                {displayUser?.firstName} {displayUser?.lastName}
                            </h1>
                            <p className="profile-email-display">{displayUser?.email}</p>
                            <span className={roleBadgeClass}>
                                {displayUser?.role ?? 'Member'}
                            </span>
                        </div>
                    </div>
                </div>

                {/* Info cards row */}
                <div className="profile-info-grid">
                    <div className="profile-info-card">
                        <div className="info-card-icon">👤</div>
                        <div className="info-card-body">
                            <label>First Name</label>
                            <p>{displayUser?.firstName || '—'}</p>
                        </div>
                    </div>
                    <div className="profile-info-card">
                        <div className="info-card-icon">👤</div>
                        <div className="info-card-body">
                            <label>Last Name</label>
                            <p>{displayUser?.lastName || '—'}</p>
                        </div>
                    </div>
                    <div className="profile-info-card">
                        <div className="info-card-icon">✉️</div>
                        <div className="info-card-body">
                            <label>Email Address</label>
                            <p>{displayUser?.email || '—'}</p>
                        </div>
                    </div>
                    <div className="profile-info-card">
                        <div className="info-card-icon">📅</div>
                        <div className="info-card-body">
                            <label>Member Since</label>
                            <p>{memberSince}</p>
                        </div>
                    </div>
                </div>

                {/* Actions */}
                <div className="profile-actions-bar">
                    <button onClick={() => navigate('/dashboard')} className="profile-btn-secondary">
                        ← Go to Dashboard
                    </button>
                    <button onClick={handleLogout} className="profile-btn-logout">
                        Sign Out
                    </button>
                </div>
            </div>
        </div>
    )
}

export default Profile
