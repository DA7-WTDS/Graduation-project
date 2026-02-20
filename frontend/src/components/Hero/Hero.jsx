import React from 'react'
import DashboardMockup from './DashboardMockup'
import './Hero.css'

const Hero = () => {
    return (
        <section className="hero">
            <div className="hero-background-decoration">
                <div className="hero-shape hero-shape-1"></div>
                <div className="hero-shape hero-shape-2"></div>
            </div>

            <div className="hero-container">
                <div className="hero-content">
                    <div className="hero-eyebrow">AI-Powered Portfolio Advisory</div>

                    <h1 className="hero-title">
                        Build Wealth with<br />
                        <span className="gradient-text">Intelligent</span> Portfolio<br />
                        Management
                    </h1>

                    <p className="hero-subtitle">
                        Let artificial intelligence analyze your portfolio.
                        Get personalized recommendations, real-time insights, and
                        data-driven guidance—all in one platform.
                    </p>

                    <div className="hero-cta-group">
                        <button className="cta-primary">
                            Get Started Free
                            <span>→</span>
                        </button>
                        <button className="cta-secondary">
                            <span>▶</span>
                            See How It Works
                        </button>
                    </div>

                    <div className="hero-trust-indicators">
                        <div className="hero-trust-item">
                            <span>✓</span>
                            Free to use
                        </div>
                        <div className="hero-trust-item">
                            <span>✓</span>
                            AI-powered insights
                        </div>
                        <div className="hero-trust-item">
                            <span>✓</span>
                            Bank-level security
                        </div>
                    </div>
                </div>

                <div className="hero-visual">
                    <DashboardMockup />
                </div>
            </div>
        </section>
    )
}

export default Hero
