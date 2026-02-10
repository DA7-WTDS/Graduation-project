import React from 'react'
import './Pricing.css'

const pricingPlans = [
    {
        name: 'Starter',
        price: '0',
        period: '/month',
        description: 'Perfect for beginners starting their investment journey',
        features: [
            'AI-powered portfolio recommendations',
            'Up to $10,000 in assets',
            'Monthly rebalancing',
            'Basic performance analytics',
            'Email support',
            'Mobile app access'
        ],
        cta: 'Start Free',
        ctaType: 'secondary'
    },
    {
        name: 'Professional',
        price: '29',
        period: '/month',
        description: 'For serious investors who want more control',
        features: [
            'Everything in Starter',
            'Unlimited assets under management',
            'Real-time rebalancing',
            'Advanced analytics & reports',
            'Goal-based planning',
            'Priority support',
            'Tax-loss harvesting',
            'Custom asset allocation'
        ],
        cta: 'Start 30-Day Trial',
        ctaType: 'primary',
        featured: true,
        badge: 'Most Popular'
    },
    {
        name: 'Enterprise',
        price: '99',
        period: '/month',
        description: 'For high-net-worth individuals and teams',
        features: [
            'Everything in Professional',
            'Dedicated account manager',
            'Multi-account management',
            'API access',
            'White-label options',
            'Custom integrations',
            'Advanced security features',
            'Institutional-grade reporting'
        ],
        cta: 'Contact Sales',
        ctaType: 'secondary'
    }
]

const Pricing = () => {
    return (
        <section id="pricing" className="pricing">
            <div className="pricing-header">
                <div className="pricing-eyebrow">PRICING</div>
                <h2 className="pricing-title">Choose Your Plan</h2>
                <p className="pricing-subtitle">Transparent pricing that grows with you. No hidden fees.</p>
            </div>

            <div className="pricing-container">
                {pricingPlans.map((plan, index) => (
                    <div key={index} className={`pricing-card ${plan.featured ? 'featured' : ''}`}>
                        {plan.badge && <div className="pricing-badge">{plan.badge}</div>}

                        <div className="pricing-card-header">
                            <h3 className="pricing-plan-name">{plan.name}</h3>
                            <div className="pricing-price">
                                <span className="pricing-currency">$</span>
                                <span className="pricing-amount">{plan.price}</span>
                                <span className="pricing-period">{plan.period}</span>
                            </div>
                            <p className="pricing-description">{plan.description}</p>
                        </div>

                        <ul className="pricing-features">
                            {plan.features.map((feature, idx) => (
                                <li key={idx} className="pricing-feature">
                                    <span className="pricing-feature-icon">✓</span>
                                    <span>{feature}</span>
                                </li>
                            ))}
                        </ul>

                        <button className={`pricing-cta ${plan.ctaType}`}>
                            {plan.cta}
                        </button>
                    </div>
                ))}
            </div>
        </section>
    )
}

export default Pricing
