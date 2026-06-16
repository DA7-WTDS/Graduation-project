import React from 'react'
import { useNavigate } from 'react-router-dom'
import { motion } from 'motion/react'
import { usePortfolio } from '@/features/portfolio/usePortfolio'
import { useNotifications, formatRelativeTime } from '@/features/notifications/useNotifications'
import { RecommendationsPanel } from '@/features/recommendations/RecommendationsPanel'
import { TargetMix } from '@/features/recommendations/TargetMix'
import { Card, StatTile, Button, LoadingState, ErrorState, EmptyState } from '@/shared/ui'
import { staggerContainer, fadeInUp } from '@/shared/motion/variants'
import './Dashboard.css'

const Dashboard = () => {
    const navigate = useNavigate()
    const { data: portfolio, isLoading, isError } = usePortfolio()
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
                    <motion.div variants={fadeInUp}><Card><LoadingState label="Loading portfolio…" /></Card></motion.div>
                ) : isError ? (
                    <motion.div variants={fadeInUp}><Card><ErrorState message="Failed to load your portfolio." /></Card></motion.div>
                ) : !portfolio ? (
                    <motion.div variants={fadeInUp}>
                        <Card className="dash-onboard">
                            <span className="dash-eyebrow">No portfolio yet</span>
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
                            <StatTile label="Risk Profile" value={portfolio.riskProfile} valueColor="var(--qw-amber)" />
                            <StatTile label="Risk Tolerance" value={`${portfolio.riskTolerance}%`} />
                            <StatTile label="Time Horizon" value={portfolio.timeHorizon} />
                            <StatTile label="Experience" value={portfolio.investmentExperience} />
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
