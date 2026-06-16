import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Palmtree, Home, GraduationCap, Coins, Zap, CalendarDays, Target, Rocket, Scale, Shield, Check } from 'lucide-react'
import { usePortfolio } from '@/features/portfolio/usePortfolio'
import { useSavePortfolio } from '@/features/portfolio/usePortfolioMutations'
import { LoadingState, useToast } from '@/shared/ui'
import './Onboarding.css'

const steps = [
    { id: 1, label: 'Goals' },
    { id: 2, label: 'Risk' },
    { id: 3, label: 'Preferences' },
    { id: 4, label: 'Portfolio' }
]

const Onboarding = () => {
    const navigate = useNavigate()
    const toast = useToast()
    // Detect an existing portfolio so "Edit Profile" / "Retake Questionnaire"
    // updates (PUT) instead of creating (POST → 409). 404 resolves to null.
    const { data: existingPortfolio, isLoading: loadingExisting } = usePortfolio()
    const savePortfolio = useSavePortfolio(existingPortfolio?.id)
    const isEditing = !!existingPortfolio

    const [currentStep, setCurrentStep] = useState(1)
    const [prefilled, setPrefilled] = useState(false)
    const [submitError, setSubmitError] = useState(null)
    const [formData, setFormData] = useState({
        // Step 1: Goals
        primaryGoal: '',
        timeHorizon: '',
        investmentAmount: '',

        // Step 2: Risk Assessment
        riskTolerance: 50,
        marketReaction: '',

        // Step 3: Preferences
        investmentExperience: '',

        // Step 4: Results
        recommendedPortfolio: null
    })

    const handleNext = () => {
        if (currentStep < 4) {
            setCurrentStep(currentStep + 1)
            return
        }

        // Step 4: create or update the portfolio, then navigate.
        const portfolio = formData.recommendedPortfolio
        if (!portfolio) return

        setSubmitError(null)
        savePortfolio.mutate(
            {
                primaryGoal: formData.primaryGoal,
                timeHorizon: formData.timeHorizon,
                riskTolerance: formData.riskTolerance,
                marketReaction: formData.marketReaction,
                investmentExperience: formData.investmentExperience,
                stocksPercentage: portfolio.stocks,
                bondsPercentage: portfolio.bonds,
                etfsPercentage: portfolio.etfs,
                cashPercentage: portfolio.cash,
                riskProfile: portfolio.risk,
                investmentAmount: parseFloat(formData.investmentAmount) || 0,
            },
            {
                onSuccess: () => {
                    toast.success(isEditing ? 'Portfolio updated' : 'Portfolio created')
                    navigate('/dashboard')
                },
                onError: (err) => {
                    const message = err.message || 'Failed to save your portfolio. Please try again.'
                    setSubmitError(message)
                    toast.error(message)
                },
            }
        )
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

    // Prefill the questionnaire from the existing portfolio (edit mode). Runs
    // once when the portfolio first resolves so it never clobbers user edits.
    React.useEffect(() => {
        if (existingPortfolio && !prefilled) {
            setFormData(prev => ({
                ...prev,
                primaryGoal: existingPortfolio.primaryGoal ?? '',
                timeHorizon: existingPortfolio.timeHorizon ?? '',
                investmentAmount: existingPortfolio.investmentAmount ? String(existingPortfolio.investmentAmount) : '',
                riskTolerance: existingPortfolio.riskTolerance ?? 50,
                marketReaction: existingPortfolio.marketReaction ?? '',
                investmentExperience: existingPortfolio.investmentExperience ?? '',
            }))
            setPrefilled(true)
        }
    }, [existingPortfolio, prefilled])

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
                return formData.primaryGoal && formData.timeHorizon && parseFloat(formData.investmentAmount) > 0
            case 2:
                return formData.marketReaction
            case 3:
                return formData.investmentExperience
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

    // Wait for the existing-portfolio probe so edit mode prefills before the
    // user starts answering (avoids a flash of empty selections).
    if (loadingExisting) {
        return (
            <div className="onboarding-page">
                <LoadingState label="Loading your profile…" />
            </div>
        )
    }

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
                            <button className="nav-button secondary" onClick={handleBack} disabled={savePortfolio.isPending}>
                                Back
                            </button>
                        )}
                        {submitError && (
                            <p style={{ color: 'var(--color-danger)', fontSize: 'var(--font-size-small)', flex: 1, textAlign: 'center' }}>
                                {submitError}
                            </p>
                        )}
                        <button
                            className="nav-button primary"
                            onClick={handleNext}
                            disabled={!isStepComplete() || savePortfolio.isPending}
                        >
                            {currentStep === 4
                                ? (savePortfolio.isPending ? 'Saving…' : (isEditing ? 'Save Changes' : 'Complete Setup'))
                                : 'Continue'}
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
        { id: 'retirement', Icon: Palmtree, title: 'Retirement', description: 'Build wealth for your golden years' },
        { id: 'property', Icon: Home, title: 'Buy Property', description: 'Save for a down payment' },
        { id: 'education', Icon: GraduationCap, title: 'Education', description: 'Fund education expenses' },
        { id: 'wealth', Icon: Coins, title: 'Build Wealth', description: 'Long-term wealth accumulation' }
    ]

    const timeHorizons = [
        { id: 'short', Icon: Zap, title: '6 Months', description: 'Very short-term goals' },
        { id: 'medium', Icon: CalendarDays, title: '1-2 Years', description: 'Medium-term planning' },
        { id: 'long', Icon: Target, title: '3+ Years', description: 'Long-term investing' }
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
                            <div className="option-icon"><goal.Icon size={26} strokeWidth={1.5} aria-hidden="true" /></div>
                            <div className="option-title">{goal.title}</div>
                            <div className="option-description">{goal.description}</div>
                        </div>
                    ))}
                </div>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">How much are you planning to invest?</h3>
                <p className="form-section-description">We'll use this to turn your AI recommendations into dollar amounts.</p>
                <div className="invest-amount-field">
                    <span className="invest-amount-prefix">$</span>
                    <input
                        type="number"
                        inputMode="decimal"
                        min="0"
                        step="100"
                        placeholder="10,000"
                        value={formData.investmentAmount}
                        onChange={(e) => onChange('investmentAmount', e.target.value)}
                        className="invest-amount-input"
                        aria-label="Amount to invest in US dollars"
                    />
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
                            <div className="option-icon"><horizon.Icon size={26} strokeWidth={1.5} aria-hidden="true" /></div>
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
        { id: 'aggressive', Icon: Rocket, title: 'Hold & Buy More', description: 'I see it as a buying opportunity' },
        { id: 'moderate', Icon: Scale, title: 'Hold Steady', description: 'I stay calm and wait it out' },
        { id: 'conservative', Icon: Shield, title: 'Sell Some', description: 'I reduce my exposure to limit losses' }
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
                            <div className="option-icon"><reaction.Icon size={26} strokeWidth={1.5} aria-hidden="true" /></div>
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

    return (
        <div className="onboarding-form">
            <div className="onboarding-header">
                <h2 className="onboarding-title">Investment Preferences</h2>
                <p className="onboarding-subtitle">
                    Tell us about your investment experience
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
        </div>
    )
}

// Step 4: Portfolio Recommendation
const Step4Portfolio = ({ formData }) => {
    const portfolio = formData.recommendedPortfolio || {}

    // Same allocation palette as the Dashboard target-mix bar.
    const allocation = [
        { label: 'Stocks', value: portfolio.stocks, color: 'var(--qw-amber)' },
        { label: 'Bonds', value: portfolio.bonds, color: 'var(--qw-amber-dim)' },
        { label: 'ETFs', value: portfolio.etfs, color: 'var(--qw-text-dim)' },
        { label: 'Cash', value: portfolio.cash, color: 'var(--qw-text-faint)' },
    ].filter(a => a.value > 0)

    const nextSteps = [
        'Your portfolio is ready to go',
        'AI will continuously monitor and optimize',
        'Automatic rebalancing included',
        'Real-time performance tracking',
    ]

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
                        {allocation.map(a => (
                            <div key={a.label} style={{ width: `${a.value}%`, background: a.color }}></div>
                        ))}
                    </div>
                    <div className="allocation-legend">
                        {allocation.map(a => (
                            <span key={a.label} className="allocation-legend-item">
                                <i style={{ background: a.color }} />{a.label} {a.value}%
                            </span>
                        ))}
                    </div>
                </div>
            </div>

            <div className="form-section">
                <h3 className="form-section-title">What's Next?</h3>
                <ul className="next-steps-list">
                    {nextSteps.map(step => (
                        <li key={step}>
                            <Check size={16} strokeWidth={2} className="next-steps-check" aria-hidden="true" />
                            {step}
                        </li>
                    ))}
                </ul>
                <p style={{ fontSize: 'var(--font-size-base)', color: 'var(--color-gray-700)', lineHeight: 1.7 }}>
                    Click "Complete Setup" to start investing with your personalized portfolio!
                </p>
            </div>
        </div>
    )
}

export default Onboarding
