import React from 'react'
import { Rocket } from 'lucide-react'
import './About.css'

const About = () => {
    return (
        <section id="about" className="about">
            <div className="about-container">
                <div className="about-header">
                    <div className="about-eyebrow">ABOUT US</div>
                    <h2 className="about-title">Democratizing Investment Intelligence</h2>
                    <p className="about-subtitle">
                        We believe everyone deserves access to professional-grade investment tools.
                        Our AI-powered platform makes sophisticated portfolio management accessible to all.
                    </p>
                </div>

                <div className="about-content">
                    <div className="about-text">
                        <h3>Our Story</h3>
                        <p>
                            QuantWise was born from a simple observation: institutional investors 
                            had access to powerful AI and data analytics, while individual investors 
                            were left behind.
                        </p>
                        <p>
                            We set out to change that. By combining cutting-edge machine learning 
                            with intuitive design, we've created a platform that puts sophisticated 
                            investment strategies in everyone's hands.
                        </p>
                    </div>

                    <div className="about-visual">
                        <div className="about-visual-icon"><Rocket size={56} strokeWidth={1.25} aria-hidden="true" /></div>
                        <h3>Innovation Meets Simplicity</h3>
                        <p style={{ color: 'var(--color-gray-600)', marginTop: 'var(--space-md)' }}>
                            Advanced AI algorithms paired with an interface anyone can use
                        </p>
                    </div>
                </div>

                <div className="about-mission">
                    <h3>Our Mission</h3>
                    <p>
                        To empower every investor with AI-driven insights and tools that were
                        once available only to Wall Street professionals. We're building a future
                        where intelligent investing is accessible, transparent, and tailored to
                        each individual's unique goals and circumstances.
                    </p>
                </div>
            </div>
        </section>
    )
}

export default About
