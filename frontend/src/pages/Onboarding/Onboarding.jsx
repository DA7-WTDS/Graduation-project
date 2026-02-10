import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import './Onboarding.css'

const steps = [
    { id: 1, label: 'Goals' },
    { id: 2, label: 'Risk' },
    { id: 3, label: 'Preferences' },
    { id: 4, label: 'Portfolio' }
]

const Onboarding = () => {
    const navigate = useNavigate()
    const [currentStep, setCurrentStep] = useState(1)
    const [formData, setFormData] = useState({
        // Step 1: Goals
        primaryGoal: '',
        timeHorizon: '',

        // Step 2: Risk Assessment
        riskTolerance: 50,
        marketReaction: '',

        // Step 3: Preferences
        investmentExperience: '',
        incomeStability: '',

        // Step 4: Results
        recommendedPortfolio: null
    })

    const handleNext = () => {
        if (currentStep < 4) {
            setCurrentStep(currentStep + 1)
        } else {
            // Complete onboarding - navigate to dashboard
            navigate('/dashboard')
        }
    }

    const handleBack = () => {
        if (currentStep > 1) {
            setCurrentStep(currentStep - 1)
        }
    }

    const handleInputChange = (field, value) => {
        setFormData(prev => ({ ...prev, [field]: value }))
    }

    const calculateRiskScore = () => {
        // Simple risk calculation based on user inputs
        const riskFactors = {
            aggressive: 90,
            moderate: 60,
            conservative: 30,
            high: 80,
            medium: 50,
            low: 20
        }

        let score = formData.riskTolerance
        if (formData.marketReaction) score = (score + riskFactors[formData.marketReaction]) / 2
        if (formData.investmentExperience) score = (score + riskFactors[formData.investmentExperience]) / 2

        return Math.round(score)
    }

    const generatePortfolio = () => {
        const riskScore = calculateRiskScore()

        // Generate allocation based on risk score
        let portfolio = {}
        if (riskScore >= 70) {
            portfolio = { stocks: 60, bonds: 20, etfs: 15, cash: 5, risk: 'Aggressive' }
        } else if (riskScore >= 40) {
            portfolio = { stocks: 40, bonds: 35, etfs: 20, cash: 5, risk: 'Moderate' }
        } else {
            portfolio = { stocks: 20, bonds: 50, etfs: 25, cash: 5, risk: 'Conservative' }
        }

        return portfolio
    }

    // Auto-generate portfolio when reaching step 4
    React.useEffect(() => {
        if (currentStep === 4 && !formData.recommendedPortfolio) {
            const portfolio = generatePortfolio()
            setFormData(prev => ({ ...prev, recommendedPortfolio: portfolio }))
        }
    }, [currentStep])

    const isStepComplete = () => {
        switch (currentStep) {
            case 1:
                return formData.primaryGoal && formData.timeHorizon
            case 2:
                return formData.marketReaction
            case 3:
                return formData.investmentExperience && formData.incomeStability
            case 4:
                return true
            default:
                return false
        }
    }

    const renderStepContent = () => {
        switch (currentStep) {
            case 1:
                return <Step1Goals formData={formData} onChange={handleInputChange} />
            case 2:
                return <Step2Risk formData={formData} onChange={handleInputChange} />
            case 3:
                return <Step3Preferences formData={formData} onChange={handleInputChange} />
            case 4:
                return <Step4Portfolio formData={formData} />
            default:
                return null
        }
    }

    const progressPercentage = ((currentStep - 1) / (steps.length - 1)) * 100

    return (
        <div className="onboarding-page">
            <div className="onboarding-container">
                {/* Progress Bar */}
                <div className="onboarding-progress">
                    <div className="progress-steps">
                        <div className="progress-line">
                            <div className="progress-line-fill" style={{ width: `${progressPercentage}%` }}></div>
                        </div>
                        {steps.map((step) => (
                            <div
                                key={step.id}
                                className={`progress-step ${step.id === currentStep ? 'active' : step.id < currentStep ? 'completed' : ''
                                    }`}
                            >
                                <div className="progress-step-circle">
                                    {step.id < currentStep ? '✓' : step.id}
                                </div>
                                <div className="progress-step-label">{step.label}</div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Step Content */}
                <div className="onboarding-card">
                    {renderStepContent()}

                    {/* Navigation */}
                    <div className="onboarding-nav">
                        {currentStep > 1 && (
                            <button className="nav-button secondary" onClick={handleBack}>
                                Back
                            </button>
                        )}
                        <button
                            className="nav-button primary"
                            onClick={handleNext}
                            disabled={!isStepComplete()}
                        >
                            {currentStep === 4 ? 'Complete Setup' : 'Continue'}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    )
}

// Step 1: Goals
const Step1Goals = ({ formData, onChange }) => {
    const goals = [
        { id: 'retirement', icon: '🏖️', title: 'Retirement', description: 'Build wealth for your golden years' },
        { id: 'property', icon: '🏠', title: 'Buy Property', description: 'Save for a down payment' },
        { id: 'education', icon: '🎓', title: 'Education', description: 'Fund education expenses' },
        { id: 'wealth', icon: '💰', title: 'Build Wealth', description: 'Long-term wealth accumulation' }
    ]

    const timeHorizons = [
        { id: 'short', icon: '⚡', title: '1-3 Years', description: 'Short-term goals' },
        { id: 'medium', icon: '📅', title: '3-7 Years', description: 'Medium-term planning' },
        { id: 'long', icon: '🎯', title: '7+ Years', description: 'Long-term investing' }
    ]

    return (
        <div className="onboarding-form">
            <div className="onboarding-header">
                <h2 className="onboarding-title">Let's Start with Your Goals</h2>
                <p className="onboarding-subtitle">
                    Understanding your financial objectives helps us create the perfect portfolio for you
                </p>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">What's your primary investment goal?</h3>
                <div className="option-grid">
                    {goals.map((goal) => (
                        <div
                            key={goal.id}
                            className={`option-card ${formData.primaryGoal === goal.id ? 'selected' : ''}`}
                            onClick={() => onChange('primaryGoal', goal.id)}
                        >
                            <input type="radio" name="primaryGoal" value={goal.id} checked={formData.primaryGoal === goal.id} readOnly />
                            <div className="option-icon">{goal.icon}</div>
                            <div className="option-title">{goal.title}</div>
                            <div className="option-description">{goal.description}</div>
                        </div>
                    ))}
                </div>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">What's your investment timeline?</h3>
                <div className="option-grid" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
                    {timeHorizons.map((horizon) => (
                        <div
                            key={horizon.id}
                            className={`option-card ${formData.timeHorizon === horizon.id ? 'selected' : ''}`}
                            onClick={() => onChange('timeHorizon', horizon.id)}
                        >
                            <input type="radio" name="timeHorizon" value={horizon.id} checked={formData.timeHorizon === horizon.id} readOnly />
                            <div className="option-icon">{horizon.icon}</div>
                            <div className="option-title">{horizon.title}</div>
                            <div className="option-description">{horizon.description}</div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    )
}

// Step 2: Risk Assessment
const Step2Risk = ({ formData, onChange }) => {
    const reactions = [
        { id: 'aggressive', icon: '🚀', title: 'Hold & Buy More', description: 'I see it as a buying opportunity' },
        { id: 'moderate', icon: '⚖️', title: 'Hold Steady', description: 'I stay calm and wait it out' },
        { id: 'conservative', icon: '🛡️', title: 'Sell Some', description: 'I reduce my exposure to limit losses' }
    ]

    return (
        <div className="onboarding-form">
            <div className="onboarding-header">
                <h2 className="onboarding-title">Understanding Your Risk Tolerance</h2>
                <p className="onboarding-subtitle">
                    This helps us match you with the right investment strategy
                </p>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">How comfortable are you with investment risk?</h3>
                <p className="form-section-description">Move the slider to indicate your comfort level</p>
                <div className="slider-container">
                    <div className="slider-labels">
                        <span className="slider-label">Conservative</span>
                        <span className="slider-label">Aggressive</span>
                    </div>
                    <input
                        type="range"
                        min="0"
                        max="100"
                        value={formData.riskTolerance}
                        onChange={(e) => onChange('riskTolerance', parseInt(e.target.value))}
                        className="slider-input"
                    />
                    <div className="slider-value">{formData.riskTolerance}%</div>
                </div>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">If the market dropped 20%, what would you do?</h3>
                <div className="option-grid">
                    {reactions.map((reaction) => (
                        <div
                            key={reaction.id}
                            className={`option-card ${formData.marketReaction === reaction.id ? 'selected' : ''}`}
                            onClick={() => onChange('marketReaction', reaction.id)}
                        >
                            <input type="radio" name="marketReaction" value={reaction.id} checked={formData.marketReaction === reaction.id} readOnly />
                            <div className="option-icon">{reaction.icon}</div>
                            <div className="option-title">{reaction.title}</div>
                            <div className="option-description">{reaction.description}</div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    )
}

// Step 3: Preferences
const Step3Preferences = ({ formData, onChange }) => {
    const experiences = [
        { id: 'high', title: 'Experienced', description: '5+ years of active investing' },
        { id: 'medium', title: 'Intermediate', description: '1-5 years of investing' },
        { id: 'low', title: 'Beginner', description: 'New to investing' }
    ]

    const incomes = [
        { id: 'high', title: 'Very Stable', description: 'Steady income, emergency fund' },
        { id: 'medium', title: 'Moderate', description: 'Regular income, some savings' },
        { id: 'low', title: 'Variable', description: 'Irregular income or expenses' }
    ]

    return (
        <div className="onboarding-form">
            <div className="onboarding-header">
                <h2 className="onboarding-title">Investment Preferences</h2>
                <p className="onboarding-subtitle">
                    A few more details to personalize your portfolio
                </p>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">What's your investment experience?</h3>
                <div className="option-grid">
                    {experiences.map((exp) => (
                        <div
                            key={exp.id}
                            className={`option-card ${formData.investmentExperience === exp.id ? 'selected' : ''}`}
                            onClick={() => onChange('investmentExperience', exp.id)}
                        >
                            <input type="radio" name="experience" value={exp.id} checked={formData.investmentExperience === exp.id} readOnly />
                            <div className="option-title">{exp.title}</div>
                            <div className="option-description">{exp.description}</div>
                        </div>
                    ))}
                </div>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">How stable is your income?</h3>
                <div className="option-grid">
                    {incomes.map((income) => (
                        <div
                            key={income.id}
                            className={`option-card ${formData.incomeStability === income.id ? 'selected' : ''}`}
                            onClick={() => onChange('incomeStability', income.id)}
                        >
                            <input type="radio" name="income" value={income.id} checked={formData.incomeStability === income.id} readOnly />
                            <div className="option-title">{income.title}</div>
                            <div className="option-description">{income.description}</div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    )
}

// Step 4: Portfolio Recommendation
const Step4Portfolio = ({ formData }) => {
    const portfolio = formData.recommendedPortfolio || {}

    return (
        <div className="onboarding-form">
            <div className="onboarding-header">
                <h2 className="onboarding-title">Your AI-Generated Portfolio</h2>
                <p className="onboarding-subtitle">
                    Based on your profile, we've created a personalized investment strategy
                </p>
            </div>

            <div className="recommendation-result">
                <div className="recommendation-title">Recommended: {portfolio.risk} Portfolio</div>

                <div className="recommendation-stats">
                    <div className="recommendation-stat">
                        <div className="stat-value">{portfolio.stocks}%</div>
                        <div className="stat-label">Stocks</div>
                    </div>
                    <div className="recommendation-stat">
                        <div className="stat-value">{portfolio.bonds}%</div>
                        <div className="stat-label">Bonds</div>
                    </div>
                    <div className="recommendation-stat">
                        <div className="stat-value">{portfolio.etfs}%</div>
                        <div className="stat-label">ETFs</div>
                    </div>
                </div>

                <div className="allocation-chart">
                    <div style={{ fontSize: '14px', marginBottom: '12px', opacity: 0.9 }}>
                        Asset Allocation
                    </div>
                    <div style={{ display: 'flex', height: '40px', borderRadius: '20px', overflow: 'hidden' }}>
                        <div style={{ width: `${portfolio.stocks}%`, background: '#6C63FF' }}></div>
                        <div style={{ width: `${portfolio.bonds}%`, background: '#00C9A7' }}></div>
                        <div style={{ width: `${portfolio.etfs}%`, background: '#0A2463' }}></div>
                        <div style={{ width: `${portfolio.cash}%`, background: 'rgba(255,255,255,0.3)' }}></div>
                    </div>
                </div>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">What's Next?</h3>
                <div style={{ fontSize: 'var(--font-size-base)', color: 'var(--color-gray-700)', lineHeight: 1.7 }}>
                    <p style={{ marginBottom: 'var(--space-md)' }}>
                        ✓ Your portfolio is ready to go<br />
                        ✓ AI will continuously monitor and optimize<br />
                        ✓ Automatic rebalancing included<br />
                        ✓ Real-time performance tracking
                    </p>
                    <p>Click "Complete Setup" to start investing with your personalized portfolio!</p>
                </div>
            </div>
        </div>
    )
}

export default Onboarding
