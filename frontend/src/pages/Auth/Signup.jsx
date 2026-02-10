import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import './Auth.css'

const Signup = () => {
    const navigate = useNavigate()
    const [formData, setFormData] = useState({
        fullName: '',
        email: '',
        password: '',
        confirmPassword: '',
        agreeToTerms: false
    })

    const handleChange = (e) => {
        const { name, value, type, checked } = e.target
        setFormData(prev => ({
            ...prev,
            [name]: type === 'checkbox' ? checked : value
        }))
    }

    const handleSubmit = (e) => {
        e.preventDefault()

        // Basic validation
        if (formData.password !== formData.confirmPassword) {
            alert('Passwords do not match!')
            return
        }

        if (!formData.agreeToTerms) {
            alert('Please agree to the terms and conditions')
            return
        }

        console.log('Signup attempt:', formData)
        // TODO: Implement actual signup logic with backend

        // Redirect to onboarding after successful signup
        navigate('/onboarding')
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
                        <h1 className="auth-title">Create Account</h1>
                        <p className="auth-subtitle">Start your investment journey today</p>
                    </div>

                    <form className="auth-form" onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label htmlFor="fullName" className="form-label">Full Name</label>
                            <input
                                type="text"
                                id="fullName"
                                name="fullName"
                                className="form-input"
                                placeholder="John Doe"
                                value={formData.fullName}
                                onChange={handleChange}
                                required
                            />
                        </div>

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
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="password" className="form-label">Password</label>
                            <input
                                type="password"
                                id="password"
                                name="password"
                                className="form-input"
                                placeholder="Create a strong password"
                                value={formData.password}
                                onChange={handleChange}
                                required
                                minLength="8"
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="confirmPassword" className="form-label">Confirm Password</label>
                            <input
                                type="password"
                                id="confirmPassword"
                                name="confirmPassword"
                                className="form-input"
                                placeholder="Confirm your password"
                                value={formData.confirmPassword}
                                onChange={handleChange}
                                required
                            />
                        </div>

                        <div className="form-checkbox-group">
                            <input
                                type="checkbox"
                                id="agreeToTerms"
                                name="agreeToTerms"
                                className="form-checkbox"
                                checked={formData.agreeToTerms}
                                onChange={handleChange}
                                required
                            />
                            <label htmlFor="agreeToTerms" className="form-checkbox-label">
                                I agree to the <span className="form-link">Terms of Service</span> and <span className="form-link">Privacy Policy</span>
                            </label>
                        </div>

                        <button type="submit" className="auth-submit">
                            Create Account
                        </button>
                    </form>

                    <div className="auth-divider">
                        <span className="auth-divider-text">or sign up with</span>
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
                        Already have an account? <Link to="/login" className="form-link">Sign in</Link>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Signup
