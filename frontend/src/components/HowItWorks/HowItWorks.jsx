import React from 'react'
import './HowItWorks.css'

const stepsData = [
    {
        number: '1',
        title: 'Create Your Profile',
        description: 'Answer a few questions about your financial goals, risk tolerance, and investment preferences.',
        icon: '📋'
    },
    {
        number: '2',
        title: 'AI Builds Your Portfolio',
        description: 'Our machine learning engine analyzes thousands of assets to create a diversified portfolio tailored to you.',
        icon: '🤖'
    },
    {
        number: '3',
        title: 'Track & Optimize',
        description: 'Monitor your performance in real-time and receive intelligent recommendations to maximize returns.',
        icon: '📊'
    }
]

const HowItWorks = () => {
    return (
        <section id="how-it-works" className="how-it-works">
            <div className="how-it-works-header">
                <div className="how-it-works-eyebrow">HOW IT WORKS</div>
                <h2 className="how-it-works-title">Get Started in 3 Simple Steps</h2>
                <p className="how-it-works-subtitle">From signup to optimized portfolio in minutes</p>
            </div>

            <div className="steps-container">
                {stepsData.map((step, index) => (
                    <div key={index} className="step">
                        <div className="step-number">{step.number}</div>
                        <h3 className="step-title">{step.title}</h3>
                        <p className="step-description">{step.description}</p>
                        <div className="step-visual">{step.icon}</div>
                    </div>
                ))}
            </div>
        </section>
    )
}

export default HowItWorks
