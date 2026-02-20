import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import './Auth.css'

const Login = () => {
    const navigate = useNavigate()
    const { login } = useAuth()
    const [formData, setFormData] = useState({
        email: '',
        password: '',
        rememberMe: false
    })
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')

    const handleChange = (e) => {
        const { name, value, type, checked } = e.target
        setFormData(prev => ({
            ...prev,
            [name]: type === 'checkbox' ? checked : value
        }))
        // Clear error when user starts typing
        if (error) setError('')
    }

    const handleSubmit = async (e) => {
        e.preventDefault()
        setError('')
        setLoading(true)

        try {
            await login(formData.email, formData.password)

            // Redirect to dashboard after successful login
            navigate('/dashboard')
        } catch (err) {
            console.error('Login error:', err)
            setError(err.message || 'Invalid email or password. Please try again.')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="auth-page">
            <div className="auth-background-decoration">
                <div className="auth-shape auth-shape-1"></div>
                <div className="auth-shape auth-shape-2"></div>
            </div>

            <Link to="/" className="back-to-home">
                <span>←</span>
                Back to Home
            </Link>

            <div className="auth-container">
                <div className="auth-card">
                    <div className="auth-logo">
                        <div className="auth-logo-text">
                            <span className="gradient-text">SmartInvest</span> AI
                        </div>
                    </div>

                    <div className="auth-header">
                        <h1 className="auth-title">Welcome Back</h1>
                        <p className="auth-subtitle">Log in to manage your portfolio</p>
                    </div>

                    {error && (
                        <div style={{
                            padding: '12px 16px',
                            marginBottom: '16px',
                            backgroundColor: '#fee',
                            border: '1px solid #fcc',
                            borderRadius: '8px',
                            color: '#c33',
                            fontSize: '14px'
                        }}>
                            {error}
                        </div>
                    )}

                    <form className="auth-form" onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label htmlFor="email" className="form-label">Email Address</label>
                            <input
                                type="email"
                                id="email"
                                name="email"
                                className="form-input"
                                placeholder="you@example.com"
                                value={formData.email}
                                onChange={handleChange}
                                required
                                disabled={loading}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="password" className="form-label">Password</label>
                            <input
                                type="password"
                                id="password"
                                name="password"
                                className="form-input"
                                placeholder="Enter your password"
                                value={formData.password}
                                onChange={handleChange}
                                required
                                disabled={loading}
                            />
                        </div>

                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <div className="form-checkbox-group">
                                <input
                                    type="checkbox"
                                    id="rememberMe"
                                    name="rememberMe"
                                    className="form-checkbox"
                                    checked={formData.rememberMe}
                                    onChange={handleChange}
                                />
                                <label htmlFor="rememberMe" className="form-checkbox-label">
                                    Remember me
                                </label>
                            </div>

                            <Link to="/forgot-password" className="form-link" style={{ fontSize: 'var(--font-size-small)' }}>
                                Forgot password?
                            </Link>
                        </div>

                        <button type="submit" className="auth-submit" disabled={loading}>
                            {loading ? 'Signing In...' : 'Sign In'}
                        </button>
                    </form>

                    <div className="auth-divider">
                        <span className="auth-divider-text">or continue with</span>
                    </div>

                    <div className="auth-social">
                        <button className="auth-social-button">
                            <span>G</span> Google
                        </button>
                        <button className="auth-social-button">
                            <span>f</span> Facebook
                        </button>
                    </div>

                    <div className="auth-footer">
                        Don't have an account? <Link to="/signup" className="form-link">Sign up</Link>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Login
