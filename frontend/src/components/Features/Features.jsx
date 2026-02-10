import React from 'react'
import './Features.css'

const featuresData = [
    {
        icon: '🧠',
        title: 'AI Portfolio Builder',
        description: 'Advanced algorithms analyze thousands of assets to build your perfect portfolio'
    },
    {
        icon: '🔄',
        title: 'Smart Rebalancing',
        description: 'Automatic portfolio adjustments to maintain optimal allocation and minimize risk'
    },
    {
        icon: '📈',
        title: 'Real-Time Insights',
        description: 'Live market data and AI-powered recommendations delivered instantly'
    },
    {
        icon: '🛡️',
        title: 'Risk Management',
        description: 'Dynamic risk assessment adapts to market conditions and your profile'
    },
    {
        icon: '🎯',
        title: 'Goal Tracking',
        description: 'Set financial goals and watch AI optimize your path to achievement'
    },
    {
        icon: '🔒',
        title: 'Bank-Level Security',
        description: '256-bit encryption, 2FA, and SOC 2 compliance protect your investments'
    }
]

const Features = () => {
    return (
        <section id="features" className="features">
            <div className="features-header">
                <div className="features-eyebrow">PLATFORM FEATURES</div>
                <h2 className="features-title">Everything You Need to Grow Your Wealth</h2>
                <p className="features-subtitle">Powered by cutting-edge AI and financial expertise</p>
            </div>

            <div className="features-grid">
                {featuresData.map((feature, index) => (
                    <div key={index} className="feature-card">
                        <div className="feature-icon">{feature.icon}</div>
                        <h3 className="feature-title">{feature.title}</h3>
                        <p className="feature-description">{feature.description}</p>
                    </div>
                ))}
            </div>
        </section>
    )
}

export default Features
