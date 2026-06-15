import React from 'react'
import { BrainCircuit, RefreshCw, TrendingUp, ShieldCheck, Target, Lock } from 'lucide-react'
import './Features.css'

const featuresData = [
    {
        Icon: BrainCircuit,
        title: 'AI Portfolio Builder',
        description: 'Advanced algorithms analyze thousands of assets to build your personalized portfolio recommendation'
    },
    {
        Icon: RefreshCw,
        title: 'Rebalancing Insights',
        description: 'AI-powered analysis identifies when your portfolio drifts from target allocation and recommends corrective actions'
    },
    {
        Icon: TrendingUp,
        title: 'Real-Time Insights',
        description: 'Live market data and AI-powered recommendations delivered instantly'
    },
    {
        Icon: ShieldCheck,
        title: 'Risk Management',
        description: 'Dynamic risk assessment adapts to market conditions and your profile'
    },
    {
        Icon: Target,
        title: 'Goal Tracking',
        description: 'Set financial goals and watch AI optimize your path to achievement'
    },
    {
        Icon: Lock,
        title: 'Bank-Level Security',
        description: '256-bit encryption, 2FA, and SOC 2 compliance protect your data and privacy'
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
                        <div className="feature-icon">
                            <feature.Icon size={24} strokeWidth={1.75} aria-hidden="true" />
                        </div>
                        <h3 className="feature-title">{feature.title}</h3>
                        <p className="feature-description">{feature.description}</p>
                    </div>
                ))}
            </div>
        </section>
    )
}

export default Features
