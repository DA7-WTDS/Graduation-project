import React from 'react'
import { useNavigate } from 'react-router-dom'
import { motion } from 'motion/react'
import { useActiveGoal } from '@/features/goals/useActiveGoal'
import { useLanguage } from '@/shared/i18n'
import { useNotifications, formatRelativeTime } from '@/features/notifications/useNotifications'
import { RecommendationsPanel } from '@/features/recommendations/RecommendationsPanel'
import { TargetMix } from '@/features/recommendations/TargetMix'
import { Card, StatTile, Button, LoadingState, ErrorState, EmptyState } from '@/shared/ui'
import { staggerContainer, fadeInUp } from '@/shared/motion/variants'
import './Dashboard.css'

const Dashboard = () => {
    const navigate = useNavigate()
    const { t } = useLanguage()
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
                    <span className="dash-eyebrow">{t('dash.eyebrow')}</span>
                    <h1 className="dash-title">{t('dash.title')}</h1>
                </motion.header>

                {isLoading ? (
                    <motion.div variants={fadeInUp}><Card><LoadingState label={t('dash.loadingGoal')} /></Card></motion.div>
                ) : isError ? (
                    <motion.div variants={fadeInUp}><Card><ErrorState message={t('dash.loadGoalFailed')} /></Card></motion.div>
                ) : !isOnboarded ? (
                    <motion.div variants={fadeInUp}>
                        <Card className="dash-onboard">
                            <span className="dash-eyebrow">{t('dash.noGoal')}</span>
                            <p className="dash-onboard-copy">{t('dash.noGoalCopy')}</p>
                            <Button variant="primary" onClick={() => navigate('/onboarding')}>
                                {t('dash.startOnboarding')}
                            </Button>
                        </Card>
                    </motion.div>
                ) : (
                    <>
                        <motion.div className="dash-tiles" variants={fadeInUp}>
                            <StatTile label={t('profile.riskProfile')} value={t(`band.${profile.riskBand}`)} valueColor="var(--qw-amber)" />
                            <StatTile label={t('profile.effectiveRisk')} value={`${profile.effectiveRisk}/100`} />
                            <StatTile label={t('profile.goal')} value={t(`goal.${goal.type}`)} />
                            <StatTile label={t('profile.horizon')} value={`${goal.horizonYears} ${t('plan.years')}`} />
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
                            <span className="dash-label">{t('dash.recentActivity')}</span>
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
                            <EmptyState title={t('dash.noActivity')} hint={t('dash.noActivityHint')} />
                        )}
                    </Card>
                </motion.div>
            </motion.div>
        </div>
    )
}

export default Dashboard
