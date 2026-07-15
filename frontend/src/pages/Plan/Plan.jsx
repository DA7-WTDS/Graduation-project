import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { motion } from 'motion/react'
import { Sparkles, Check, ShieldAlert, TrendingUp } from 'lucide-react'
import { useGoals } from '@/features/goals/useGoals'
import { useProposals, useCreateProposal, useAcceptProposal } from '@/features/goals/useProposals'
import { useGoalPortfolio } from '@/features/goals/useGoalPortfolio'
import { useTrackRecord } from '@/features/goals/useTrackRecord'
import { Card, Button, StatTile, LoadingState, ErrorState, EmptyState, useToast } from '@/shared/ui'
import { staggerContainer, fadeInUp } from '@/shared/motion/variants'
import { PortfolioCard } from './PortfolioCard'
import { TrackRecordCard } from './TrackRecordCard'
import './Plan.css'

const GOAL_LABELS = {
    Retirement: 'Retirement',
    LongTermWealth: 'Long-term wealth',
    MediumTermGoal: 'Medium-term goal',
    SpeculationLearning: 'Speculation & learning',
}

const SLEEVE_COLORS = {
    core: 'var(--qw-amber)',
    tactical: 'var(--qw-amber-dim, #b8860b)',
    stability: 'var(--qw-text-dim, #8a94a6)',
    speculative: 'var(--qw-text-faint, #5a6270)',
}

const money = (n) => `$${Number(n).toLocaleString(undefined, { maximumFractionDigits: 0 })}`
const pct = (n) => `${(Number(n) * 100).toFixed(1)}%`

const Plan = () => {
    const navigate = useNavigate()
    const toast = useToast()
    const { data: goals, isLoading, isError } = useGoals()
    const goal = goals?.[0] ?? null
    const goalId = goal?.id

    const { data: proposals } = useProposals(goalId)
    const { data: livePortfolio } = useGoalPortfolio(goalId)
    const { data: trackRecord } = useTrackRecord()
    const createProposal = useCreateProposal(goalId)
    const acceptProposal = useAcceptProposal(goalId)

    // The proposal currently on screen: the freshly generated / latest one.
    const [activeId, setActiveId] = useState(null)
    const accepted = proposals?.find((p) => p.status === 'Accepted') ?? null
    const shown =
        proposals?.find((p) => p.id === activeId) ??
        proposals?.[0] ??
        null

    const handleGenerate = () => {
        createProposal.mutate(undefined, {
            onSuccess: (p) => {
                setActiveId(p.id)
                toast.success(`Proposal v${p.version} generated`)
            },
            onError: (err) => toast.error(err.message || 'Could not generate a proposal.'),
        })
    }

    const handleAccept = (proposalId) => {
        acceptProposal.mutate(proposalId, {
            onSuccess: (p) => toast.success(`Accepted — v${p.version} is now your plan`),
            onError: (err) => toast.error(err.message || 'Could not accept this proposal.'),
        })
    }

    if (isLoading) {
        return <div className="plan"><Card><LoadingState label="Loading your goal…" /></Card></div>
    }
    if (isError) {
        return <div className="plan"><Card><ErrorState message="Failed to load your goal." /></Card></div>
    }
    if (!goal || !goal.profile) {
        return (
            <div className="plan">
                <Card className="plan-onboard">
                    <span className="plan-eyebrow">No goal yet</span>
                    <p className="plan-onboard-copy">
                        Answer the questionnaire and we'll build a portfolio proposal tuned to your goal.
                    </p>
                    <Button variant="primary" onClick={() => navigate('/onboarding')}>Start questionnaire</Button>
                </Card>
            </div>
        )
    }

    const profile = goal.profile

    return (
        <div className="plan">
            <motion.div className="plan-inner" variants={staggerContainer} initial="hidden" animate="show">
                <motion.header className="plan-head" variants={fadeInUp}>
                    <span className="plan-eyebrow">Your plan</span>
                    <h1 className="plan-title">{GOAL_LABELS[goal.type] ?? goal.type}</h1>
                    <p className="plan-sub">{goal.horizonYears}-year horizon · {profile.engagement} engagement</p>
                </motion.header>

                {/* Investor profile */}
                <motion.div className="plan-tiles" variants={fadeInUp}>
                    <StatTile label="Risk Profile" value={profile.riskBand} valueColor="var(--qw-amber)" />
                    <StatTile label="Effective Risk" value={`${profile.effectiveRisk}/100`} />
                    <StatTile label="Capacity" value={`${profile.capacity}/100`} />
                    <StatTile label="Tolerance" value={`${profile.tolerance}/100`} />
                </motion.div>

                {profile.speculativeUnlocked && (
                    <motion.p className="plan-spec" variants={fadeInUp}>
                        <TrendingUp size={14} aria-hidden="true" /> Speculative opportunities unlocked — always capped and clearly labeled.
                    </motion.p>
                )}

                {/* Live portfolio — only once something has been accepted */}
                {livePortfolio && (
                    <motion.div variants={fadeInUp}>
                        <PortfolioCard portfolio={livePortfolio} />
                    </motion.div>
                )}

                {/* Generate / review */}
                <motion.div variants={fadeInUp}>
                    <Card className="plan-proposal">
                        <div className="plan-card-head">
                            <span className="plan-label">Portfolio proposal</span>
                            <Button
                                variant={shown ? 'secondary' : 'primary'}
                                onClick={handleGenerate}
                                disabled={createProposal.isPending}
                            >
                                <Sparkles size={15} strokeWidth={2} aria-hidden="true" />
                                {createProposal.isPending ? 'Generating…' : shown ? 'Regenerate' : 'Generate proposal'}
                            </Button>
                        </div>

                        {!shown ? (
                            <EmptyState
                                title="No proposal yet"
                                hint="Generate one to see a deterministic, risk-graded allocation for this goal."
                            />
                        ) : (
                            <ProposalView
                                proposal={shown}
                                isAccepted={shown.status === 'Accepted'}
                                onAccept={() => handleAccept(shown.id)}
                                accepting={acceptProposal.isPending}
                            />
                        )}
                    </Card>
                </motion.div>

                {/* Our realized track record — shown unedited, good or bad */}
                {trackRecord && (
                    <motion.div variants={fadeInUp}>
                        <TrackRecordCard trackRecord={trackRecord} />
                    </motion.div>
                )}

                {/* History */}
                {proposals && proposals.length > 0 && (
                    <motion.div variants={fadeInUp}>
                        <Card className="plan-history">
                            <div className="plan-card-head">
                                <span className="plan-label">Proposal history</span>
                                {accepted && <span className="plan-accepted-note">v{accepted.version} accepted</span>}
                            </div>
                            <div className="plan-history-list">
                                {proposals.map((p) => (
                                    <button
                                        type="button"
                                        key={p.id}
                                        className={`plan-history-row${p.id === shown?.id ? ' active' : ''}`}
                                        onClick={() => setActiveId(p.id)}
                                    >
                                        <span className="plan-history-v">v{p.version}</span>
                                        <span className="plan-history-tpl">{p.templateName}</span>
                                        <StatusBadge status={p.status} />
                                        <span className="plan-history-date">
                                            {new Date(p.createdAt).toLocaleDateString()}
                                        </span>
                                    </button>
                                ))}
                            </div>
                        </Card>
                    </motion.div>
                )}
            </motion.div>
        </div>
    )
}

const StatusBadge = ({ status }) => (
    <span className={`plan-badge plan-badge-${status.toLowerCase()}`}>{status}</span>
)

const ProposalView = ({ proposal, isAccepted, onAccept, accepting }) => {
    const positions = [...proposal.positions].sort((a, b) => b.weight - a.weight)

    return (
        <div className="plan-proposal-body">
            <div className="plan-proposal-meta">
                <div>
                    <span className="plan-tpl-name">{proposal.templateName}</span>
                    <span className="plan-tpl-sub">
                        v{proposal.version} · rebalance {proposal.rebalanceCadence.replace('_', '-')} ·
                        drawdown alert at {pct(proposal.drawdownAlertPct)}
                    </span>
                </div>
                <StatusBadge status={proposal.status} />
            </div>

            {/* Allocation bar */}
            <div className="plan-alloc-bar">
                {positions.map((p) => (
                    <div
                        key={p.symbol}
                        className="plan-alloc-seg"
                        style={{ width: `${p.weight * 100}%`, background: SLEEVE_COLORS[p.sleeve] ?? 'var(--qw-text-faint)' }}
                        title={`${p.symbol} ${pct(p.weight)}`}
                    />
                ))}
            </div>

            {/* Positions */}
            <div className="plan-positions">
                {positions.map((p) => (
                    <div className="plan-pos" key={p.symbol}>
                        <span className="plan-pos-dot" style={{ background: SLEEVE_COLORS[p.sleeve] ?? 'var(--qw-text-faint)' }} />
                        <span className="plan-pos-sym">{p.symbol}</span>
                        <span className="plan-pos-sleeve">{p.sleeve}</span>
                        <span className="plan-pos-rationale">{p.rationale}</span>
                        <span className="plan-pos-weight">{pct(p.weight)}</span>
                        <span className="plan-pos-value">{money(p.estimatedValue)}</span>
                    </div>
                ))}
            </div>

            {proposal.assumptions.length > 0 && (
                <ul className="plan-assumptions">
                    {proposal.assumptions.map((a, i) => (
                        <li key={i}><ShieldAlert size={13} aria-hidden="true" /> {a}</li>
                    ))}
                </ul>
            )}

            <div className="plan-proposal-foot">
                <span className="plan-hash" title={proposal.inputsHash}>
                    audit #{proposal.inputsHash.slice(0, 10)}
                </span>
                {isAccepted ? (
                    <span className="plan-accepted-flag"><Check size={15} strokeWidth={2.5} aria-hidden="true" /> Accepted plan</span>
                ) : proposal.status === 'Superseded' ? (
                    <span className="plan-superseded-flag">Superseded — generate a fresh one</span>
                ) : (
                    <Button variant="primary" onClick={onAccept} disabled={accepting}>
                        {accepting ? 'Accepting…' : 'Accept this plan'}
                    </Button>
                )}
            </div>
        </div>
    )
}

export default Plan
