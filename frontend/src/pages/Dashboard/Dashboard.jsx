import React from 'react'
import { useNavigate } from 'react-router-dom'
import { motion } from 'motion/react'
import { useActiveGoal } from '@/features/goals/useActiveGoal'
import { useNotifications, formatRelativeTime } from '@/features/notifications/useNotifications'
import { RecommendationsPanel } from '@/features/recommendations/RecommendationsPanel'
import { TargetMix } from '@/features/recommendations/TargetMix'
import { Card, StatTile, Button, LoadingState, ErrorState, EmptyState } from '@/shared/ui'
import { staggerContainer, fadeInUp } from '@/shared/motion/variants'
import './Dashboard.css'

const GOAL_LABELS = {
    Retirement: 'Retirement',
    LongTermWealth: 'Long-term wealth',
    MediumTermGoal: 'Medium-term goal',
    SpeculationLearning: 'Speculation & learning',
}

const Dashboard = () => {
    const navigate = useNavigate()
    const { goal, profile, isOnboarded, isLoading, isError } = useActiveGoal()
    const { notifications, markAsRead } = useNotifications()

    return (
        <div className="dash">
            <motion.div
                className="dash-inner"
                variants={staggerContainer}
                initial="hidden"
                animate="show"
            >
                <motion.header className="dash-head" variants={fadeInUp}>
                    <span className="dash-eyebrow">Today's readout</span>
                    <h1 className="dash-title">Dashboard</h1>
                </motion.header>

                {isLoading ? (
                    <motion.div variants={fadeInUp}><Card><LoadingState label="Loading your goal…" /></Card></motion.div>
                ) : isError ? (
                    <motion.div variants={fadeInUp}><Card><ErrorState message="Failed to load your goal." /></Card></motion.div>
                ) : !isOnboarded ? (
                    <motion.div variants={fadeInUp}>
                        <Card className="dash-onboard">
                            <span className="dash-eyebrow">No goal yet</span>
                            <p className="dash-onboard-copy">
                                Complete the questionnaire to get personalized, risk-graded picks.
                            </p>
                            <Button variant="primary" onClick={() => navigate('/onboarding')}>
                                Start onboarding
                            </Button>
                        </Card>
                    </motion.div>
                ) : (
                    <>
                        <motion.div className="dash-tiles" variants={fadeInUp}>
                            <StatTile label="Risk Profile" value={profile.riskBand} valueColor="var(--qw-amber)" />
                            <StatTile label="Effective Risk" value={`${profile.effectiveRisk}/100`} />
                            <StatTile label="Goal" value={GOAL_LABELS[goal.type] ?? goal.type} />
                            <StatTile label="Horizon" value={`${goal.horizonYears} years`} />
                        </motion.div>

                        <motion.div variants={fadeInUp}>
                            <Card>
                                <TargetMix />
                            </Card>
                        </motion.div>
                    </>
                )}

                <motion.div className="dash-grid" variants={fadeInUp}>
                    <Card className="dash-recs">
                        <RecommendationsPanel />
                    </Card>

                    <Card className="dash-activity">
                        <div className="dash-card-head">
                            <span className="dash-label">Recent Activity</span>
                        </div>
                        {notifications.length > 0 ? (
                            <div className="dash-activity-list">
                                {notifications.slice(0, 6).map((n, i) => (
                                    <button
                                        type="button"
                                        key={n.id || i}
                                        className={`dash-activity-item${n.isRead ? '' : ' unread'}`}
                                        onClick={() => !n.isRead && markAsRead(n.id)}
                                    >
                                        <span className="dash-activity-dot" aria-hidden="true" />
                                        <span className="dash-activity-body">
                                            <span className="dash-activity-title">{n.title}</span>
                                            <span className="dash-activity-msg">{n.message}</span>
                                            <span className="dash-activity-time">{formatRelativeTime(n.createdAt)}</span>
                                        </span>
                                    </button>
                                ))}
                            </div>
                        ) : (
                            <EmptyState title="No activity yet" hint="Notifications will appear here." />
                        )}
                    </Card>
                </motion.div>
            </motion.div>
        </div>
    )
}

export default Dashboard
