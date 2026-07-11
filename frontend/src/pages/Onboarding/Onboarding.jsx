import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
    Palmtree, Coins, Target, Rocket, Scale, Shield, ShieldAlert, Check,
    Zap, CalendarDays, Landmark, Wallet, TrendingDown, GraduationCap,
    BellRing, CalendarClock, Armchair, DollarSign, Banknote, CircleDollarSign
} from 'lucide-react'
import { useGoals, useSubmitQuestionnaire } from '@/features/goals/useGoals'
import { LoadingState, useToast } from '@/shared/ui'
import './Onboarding.css'

// Phase 2 questionnaire (§ 2.1): ten questions, one per screen, raw answers only.
// All scoring happens server-side (versioned engine); the client renders the
// returned profile and never computes risk itself.

const phases = [
    { id: 1, label: 'Goal' },
    { id: 2, label: 'Capacity' },
    { id: 3, label: 'Tolerance' },
    { id: 4, label: 'Style' },
    { id: 5, label: 'Profile' },
]

// question index (1-based) → progress phase
const phaseOf = (q) => (q <= 3 ? 1 : q <= 6 ? 2 : q <= 8 ? 3 : q <= 10 ? 4 : 5)

const TOTAL_QUESTIONS = 10

const Onboarding = () => {
    const navigate = useNavigate()
    const toast = useToast()

    // Existing goal → retake mode: the server appends a new profile version
    // instead of creating a second goal (v1 UI is single-goal).
    const { data: goals, isLoading: loadingGoals } = useGoals()
    const existingGoal = goals?.[0] ?? null
    const submitQuestionnaire = useSubmitQuestionnaire()

    const [question, setQuestion] = useState(1)
    const [result, setResult] = useState(null)
    const [submitError, setSubmitError] = useState(null)
    const [answers, setAnswers] = useState({
        goalType: '',
        horizonYears: null,
        investmentAmount: '',
        monthlyContribution: '',
        hasEmergencyFund: null,
        incomeStability: '',
        savingsShare: '',
        marketReaction: '',
        experience: '',
        affordLossConfirmed: false,
        engagement: '',
        usdComfort: '',
    })

    const setAnswer = (field, value) => setAnswers(prev => ({ ...prev, [field]: value }))

    const handleSubmit = () => {
        setSubmitError(null)
        submitQuestionnaire.mutate(
            {
                goalId: existingGoal?.id ?? null,
                goalType: answers.goalType,
                horizonYears: answers.horizonYears ?? 0,
                investmentAmount: parseFloat(answers.investmentAmount) || 0,
                monthlyContribution: parseFloat(answers.monthlyContribution) || 0,
                hasEmergencyFund: answers.hasEmergencyFund === true,
                incomeStability: answers.incomeStability,
                savingsShare: answers.savingsShare,
                marketReaction: answers.marketReaction,
                experience: answers.experience,
                engagement: answers.engagement,
                usdComfort: answers.usdComfort,
                affordLossConfirmed: answers.affordLossConfirmed,
            },
            {
                onSuccess: (profile) => {
                    setResult(profile)
                    setQuestion(TOTAL_QUESTIONS + 1)
                    toast.success(existingGoal ? 'Profile updated' : 'Profile created')
                },
                onError: (err) => {
                    const message = err.message || 'Failed to save your answers. Please try again.'
                    setSubmitError(message)
                    toast.error(message)
                },
            },
        )
    }

    const handleNext = () => {
        if (question < TOTAL_QUESTIONS) {
            setQuestion(question + 1)
        } else if (question === TOTAL_QUESTIONS) {
            handleSubmit()
        } else {
            navigate('/dashboard')
        }
    }

    const handleBack = () => {
        if (question > 1 && question <= TOTAL_QUESTIONS) setQuestion(question - 1)
    }

    const isAnswered = () => {
        switch (question) {
            case 1: return !!answers.goalType
            case 2: return answers.horizonYears !== null
            case 3: return parseFloat(answers.investmentAmount) > 0
            case 4: return answers.hasEmergencyFund !== null
            case 5: return !!answers.incomeStability
            case 6: return !!answers.savingsShare
            case 7: return !!answers.marketReaction
            case 8: return !!answers.experience
            case 9: return !!answers.engagement
            case 10: return !!answers.usdComfort
            default: return true
        }
    }

    const currentPhase = phaseOf(question)
    const progressPercentage = ((currentPhase - 1) / (phases.length - 1)) * 100

    if (loadingGoals) {
        return (
            <div className="onboarding-page">
                <LoadingState label="Loading your profile…" />
            </div>
        )
    }

    return (
        <div className="onboarding-page">
            <div className="onboarding-container">
                <div className="onboarding-progress">
                    <div className="progress-steps">
                        <div className="progress-line">
                            <div className="progress-line-fill" style={{ width: `${progressPercentage}%` }}></div>
                        </div>
                        {phases.map((phase) => (
                            <div
                                key={phase.id}
                                className={`progress-step ${phase.id === currentPhase ? 'active' : phase.id < currentPhase ? 'completed' : ''}`}
                            >
                                <div className="progress-step-circle">
                                    {phase.id < currentPhase ? '✓' : phase.id}
                                </div>
                                <div className="progress-step-label">{phase.label}</div>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="onboarding-card">
                    <QuestionScreen question={question} answers={answers} onChange={setAnswer} result={result} />

                    <div className="onboarding-nav">
                        {question > 1 && question <= TOTAL_QUESTIONS && (
                            <button className="nav-button secondary" onClick={handleBack} disabled={submitQuestionnaire.isPending}>
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
                            disabled={!isAnswered() || submitQuestionnaire.isPending}
                        >
                            {question > TOTAL_QUESTIONS
                                ? 'Go to Dashboard'
                                : question === TOTAL_QUESTIONS
                                    ? (submitQuestionnaire.isPending ? 'Scoring…' : 'See My Profile')
                                    : `Continue  ·  ${question}/${TOTAL_QUESTIONS}`}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    )
}

// ---------- shared card list ----------

const OptionCards = ({ options, value, onSelect, columns }) => (
    <div className="option-grid" style={columns ? { gridTemplateColumns: `repeat(${columns}, 1fr)` } : undefined}>
        {options.map((opt) => (
            <div
                key={String(opt.id)}
                className={`option-card ${value === opt.id ? 'selected' : ''}`}
                onClick={() => onSelect(opt.id)}
            >
                <input type="radio" checked={value === opt.id} readOnly />
                {opt.Icon && <div className="option-icon"><opt.Icon size={26} strokeWidth={1.5} aria-hidden="true" /></div>}
                <div className="option-title">{opt.title}</div>
                {opt.description && <div className="option-description">{opt.description}</div>}
            </div>
        ))}
    </div>
)

const Screen = ({ title, subtitle, children }) => (
    <div className="onboarding-form">
        <div className="onboarding-header">
            <h2 className="onboarding-title">{title}</h2>
            {subtitle && <p className="onboarding-subtitle">{subtitle}</p>}
        </div>
        <div className="form-section">{children}</div>
    </div>
)

// ---------- the ten questions ----------

const QuestionScreen = ({ question, answers, onChange, result }) => {
    switch (question) {
        case 1:
            return (
                <Screen title="What is this money for?" subtitle="Your goal drives everything — strategy, monitoring, and how we talk to you.">
                    <OptionCards
                        value={answers.goalType}
                        onSelect={(v) => onChange('goalType', v)}
                        options={[
                            { id: 'retirement', Icon: Palmtree, title: 'Retirement', description: 'Set-and-forget wealth for later in life' },
                            { id: 'long_term_wealth', Icon: Coins, title: 'Long-term wealth', description: 'Grow capital over many years' },
                            { id: 'medium_term_goal', Icon: Target, title: 'A medium-term goal', description: 'Home, wedding, education — a few years out' },
                            { id: 'speculation_learning', Icon: Rocket, title: 'Speculation & learning', description: 'Active, higher-risk investing' },
                        ]}
                    />
                </Screen>
            )
        case 2:
            return (
                <Screen title="When will you need this money?" subtitle="Longer horizons can ride out downturns — that changes what's suitable.">
                    <OptionCards
                        value={answers.horizonYears}
                        onSelect={(v) => onChange('horizonYears', v)}
                        columns={5}
                        options={[
                            { id: 0, Icon: Zap, title: '< 1 year' },
                            { id: 1, Icon: CalendarDays, title: '1–2 years' },
                            { id: 3, Icon: CalendarDays, title: '3–4 years' },
                            { id: 5, Icon: Target, title: '5–9 years' },
                            { id: 10, Icon: Palmtree, title: '10+ years' },
                        ]}
                    />
                </Screen>
            )
        case 3:
            return (
                <Screen title="How much are you investing?" subtitle="We use this for position sizing — minimums, fractions, and per-pick amounts.">
                    <h3 className="form-section-title">Starting amount</h3>
                    <div className="invest-amount-field">
                        <span className="invest-amount-prefix">$</span>
                        <input
                            type="number" inputMode="decimal" min="0" step="100" placeholder="10,000"
                            value={answers.investmentAmount}
                            onChange={(e) => onChange('investmentAmount', e.target.value)}
                            className="invest-amount-input"
                            aria-label="Starting amount"
                        />
                    </div>
                    <h3 className="form-section-title" style={{ marginTop: 'var(--space-6, 24px)' }}>Monthly top-up (optional)</h3>
                    <div className="invest-amount-field">
                        <span className="invest-amount-prefix">$</span>
                        <input
                            type="number" inputMode="decimal" min="0" step="50" placeholder="0"
                            value={answers.monthlyContribution}
                            onChange={(e) => onChange('monthlyContribution', e.target.value)}
                            className="invest-amount-input"
                            aria-label="Monthly contribution"
                        />
                    </div>
                </Screen>
            )
        case 4:
            return (
                <Screen title="Do you have an emergency fund?" subtitle="Cash covering 3–6 months of expenses, separate from this investment.">
                    <OptionCards
                        value={answers.hasEmergencyFund}
                        onSelect={(v) => onChange('hasEmergencyFund', v)}
                        columns={2}
                        options={[
                            { id: true, Icon: Shield, title: 'Yes', description: 'I have a separate safety cushion' },
                            { id: false, Icon: ShieldAlert, title: 'No', description: 'This is most of my available cash' },
                        ]}
                    />
                </Screen>
            )
        case 5:
            return (
                <Screen title="How stable is your income?" subtitle="Steady income means losses can be recovered from — that raises capacity.">
                    <OptionCards
                        value={answers.incomeStability}
                        onSelect={(v) => onChange('incomeStability', v)}
                        columns={3}
                        options={[
                            { id: 'stable', Icon: Landmark, title: 'Stable', description: 'Salary or reliable recurring income' },
                            { id: 'variable', Icon: TrendingDown, title: 'Variable', description: 'Freelance, commission, seasonal' },
                            { id: 'none', Icon: Wallet, title: 'No income', description: 'Student, between jobs, retired' },
                        ]}
                    />
                </Screen>
            )
        case 6:
            return (
                <Screen title="How much of your total savings is this?" subtitle="Investing most of what you have calls for a more careful plan.">
                    <OptionCards
                        value={answers.savingsShare}
                        onSelect={(v) => onChange('savingsShare', v)}
                        columns={4}
                        options={[
                            { id: 'less_than_ten_percent', title: 'Under 10%' },
                            { id: 'ten_to_twenty_five_percent', title: '10–25%' },
                            { id: 'twenty_five_to_fifty_percent', title: '25–50%' },
                            { id: 'more_than_fifty_percent', title: 'Over 50%' },
                        ]}
                    />
                </Screen>
            )
        case 7:
            return (
                <Screen title="The market drops 20%. What do you do?" subtitle="Be honest — this is about how you'd actually feel, not the 'right' answer.">
                    <OptionCards
                        value={answers.marketReaction}
                        onSelect={(v) => onChange('marketReaction', v)}
                        options={[
                            { id: 'buy_more', Icon: Rocket, title: 'Buy more', description: 'Prices are on sale — a buying opportunity' },
                            { id: 'hold_steady', Icon: Scale, title: 'Hold steady', description: 'Stay calm and wait it out' },
                            { id: 'sell_some', Icon: Shield, title: 'Sell some', description: 'Reduce exposure to limit further losses' },
                            { id: 'sell_all', Icon: ShieldAlert, title: 'Sell everything', description: "I couldn't sleep — I'd get out" },
                        ]}
                    />
                </Screen>
            )
        case 8:
            return (
                <Screen title="How much investing experience do you have?">
                    <OptionCards
                        value={answers.experience}
                        onSelect={(v) => onChange('experience', v)}
                        options={[
                            { id: 'none', title: 'None', description: 'This is my first time' },
                            { id: 'beginner', Icon: GraduationCap, title: 'Beginner', description: 'Under a year of investing' },
                            { id: 'intermediate', title: 'Intermediate', description: '1–5 years of investing' },
                            { id: 'experienced', title: 'Experienced', description: '5+ years of active investing' },
                        ]}
                    />
                    {(answers.experience === 'intermediate' || answers.experience === 'experienced') && (
                        <label style={{
                            display: 'flex', alignItems: 'flex-start', gap: '10px',
                            marginTop: 'var(--space-5, 20px)', cursor: 'pointer',
                            fontSize: 'var(--font-size-small)', lineHeight: 1.6,
                        }}>
                            <input
                                type="checkbox"
                                checked={answers.affordLossConfirmed}
                                onChange={(e) => onChange('affordLossConfirmed', e.target.checked)}
                                style={{ marginTop: '3px' }}
                            />
                            <span>
                                Unlock <strong>speculative opportunities</strong> (IPOs, catalysts). I confirm this is
                                money I can afford to lose entirely.
                            </span>
                        </label>
                    )}
                </Screen>
            )
        case 9:
            return (
                <Screen title="How involved do you want to be?" subtitle="This sets your notification cadence — we won't ping a set-and-forget investor daily.">
                    <OptionCards
                        value={answers.engagement}
                        onSelect={(v) => onChange('engagement', v)}
                        columns={3}
                        options={[
                            { id: 'daily', Icon: BellRing, title: 'Daily', description: 'I want signals and updates every day' },
                            { id: 'monthly', Icon: CalendarClock, title: 'Monthly', description: 'A monthly review works for me' },
                            { id: 'set_and_forget', Icon: Armchair, title: 'Set & forget', description: 'Only alert me when it truly matters' },
                        ]}
                    />
                </Screen>
            )
        case 10:
            return (
                <Screen title="How do you feel about holding US-dollar assets?" subtitle="USD exposure can hedge EGP devaluation but adds currency considerations.">
                    <OptionCards
                        value={answers.usdComfort}
                        onSelect={(v) => onChange('usdComfort', v)}
                        columns={3}
                        options={[
                            { id: 'comfortable', Icon: DollarSign, title: 'Comfortable', description: 'I want USD assets as a hedge' },
                            { id: 'neutral', Icon: CircleDollarSign, title: 'No preference', description: 'Whatever fits my plan best' },
                            { id: 'prefer_egp', Icon: Banknote, title: 'Prefer EGP', description: 'Keep me mostly in local assets' },
                        ]}
                    />
                </Screen>
            )
        default:
            return <ResultScreen result={result} />
    }
}

// ---------- result: the server-scored profile ----------

const Meter = ({ label, value }) => (
    <div style={{ marginBottom: '14px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', marginBottom: '6px', opacity: 0.9 }}>
            <span>{label}</span>
            <span>{value}/100</span>
        </div>
        <div style={{ height: '8px', borderRadius: '4px', background: 'var(--qw-border, rgba(255,255,255,0.08))', overflow: 'hidden' }}>
            <div style={{ width: `${value}%`, height: '100%', background: 'var(--qw-amber)', borderRadius: '4px' }} />
        </div>
    </div>
)

const ResultScreen = ({ result }) => {
    if (!result) return null

    const allocation = [
        { label: 'Stocks', value: result.stocksPercentage, color: 'var(--qw-amber)' },
        { label: 'Bonds', value: result.bondsPercentage, color: 'var(--qw-amber-dim)' },
        { label: 'ETFs', value: result.etfsPercentage, color: 'var(--qw-text-dim)' },
        { label: 'Cash', value: result.cashPercentage, color: 'var(--qw-text-faint)' },
    ].filter(a => a.value > 0)

    const explanation = result.capacity < result.tolerance
        ? 'Your financial situation sets the ceiling here — your appetite for risk is higher than what your circumstances can safely absorb right now.'
        : result.tolerance < result.capacity
            ? 'Your comfort with risk sets the ceiling here — your finances could take more, but a plan you can stick with matters more.'
            : 'Your financial capacity and risk appetite are in balance.'

    return (
        <div className="onboarding-form">
            <div className="onboarding-header">
                <h2 className="onboarding-title">Your Investor Profile</h2>
                <p className="onboarding-subtitle">
                    Scored server-side (engine {result.scoringVersion}, profile v{result.profileVersion}) — your answers are kept on record, unchanged.
                </p>
            </div>

            <div className="recommendation-result">
                <div className="recommendation-title">Risk profile: {result.riskBand}</div>

                <div style={{ margin: '18px 0' }}>
                    <Meter label="Risk capacity (your finances)" value={result.capacity} />
                    <Meter label="Risk tolerance (your temperament)" value={result.tolerance} />
                    <Meter label="Effective risk (the lower of the two)" value={result.effectiveRisk} />
                </div>

                <p style={{ fontSize: '13px', opacity: 0.85, lineHeight: 1.6, marginBottom: '18px' }}>{explanation}</p>

                {result.speculativeUnlocked && (
                    <p style={{ fontSize: '13px', marginBottom: '18px' }}>
                        <Rocket size={14} style={{ verticalAlign: '-2px' }} aria-hidden="true" />{' '}
                        Speculative opportunities unlocked — always capped and clearly labeled.
                    </p>
                )}

                <div className="allocation-chart">
                    <div style={{ fontSize: '14px', marginBottom: '12px', opacity: 0.9 }}>Starting Allocation</div>
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
                <ul className="next-steps-list">
                    {[
                        'Daily AI signals tuned to your risk band',
                        'We track our real hit rate publicly — no hindsight editing',
                        'Retake the questionnaire anytime; every version is kept',
                    ].map(step => (
                        <li key={step}>
                            <Check size={16} strokeWidth={2} className="next-steps-check" aria-hidden="true" />
                            {step}
                        </li>
                    ))}
                </ul>
            </div>
        </div>
    )
}

export default Onboarding
