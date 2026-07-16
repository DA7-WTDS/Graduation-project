import React from 'react'
import { Target } from 'lucide-react'
import { Card, StatTile, EmptyState } from '@/shared/ui'
import { useLanguage } from '@/shared/i18n'

const pct = (n) => `${Number(n).toFixed(1)}%`
const signedPct = (n) => `${n >= 0 ? '+' : ''}${Number(n).toFixed(2)}%`

/**
 * Our realized track record (§ 0.3) — the honest answer to "how right have you
 * been?", shown to the user unedited. Predictions are scored against what the
 * market actually did 30 days later; nothing here is back-fitted, and a bad
 * number stays on screen.
 */
export const TrackRecordCard = ({ trackRecord }) => {
    const { t } = useLanguage()
    const window = trackRecord.windows?.find((w) => w.windowDays === 90) ?? trackRecord.windows?.[0]

    if (!window || window.count === 0) {
        return (
            <Card className="plan-track">
                <div className="plan-card-head">
                    <span className="plan-label">{t('track.title')}</span>
                </div>
                <EmptyState title={t('track.empty')} hint={t('track.emptyHint')} />
            </Card>
        )
    }

    return (
        <Card className="plan-track">
            <div className="plan-card-head">
                <span className="plan-label">{t('track.title')}</span>
                <span className="plan-track-window">{window.windowDays}d · {window.count}</span>
            </div>

            <div className="plan-tiles">
                <StatTile label={t('track.hitRate')} value={pct(window.hitRatePct)} valueColor="var(--qw-amber)" />
                <StatTile
                    label={t('track.avgReturn')}
                    value={signedPct(window.avgRealizedReturnPct)}
                    valueColor={window.avgRealizedReturnPct >= 0 ? 'var(--color-success)' : 'var(--color-danger)'}
                />
                <StatTile label={t('track.scored')} value={window.count.toLocaleString()} />
                <StatTile label={t('track.allTime')} value={trackRecord.totalScored.toLocaleString()} />
            </div>

            {window.byRiskLevel?.length > 0 && (
                <div className="plan-track-buckets">
                    {window.byRiskLevel.map((b) => (
                        <div className="plan-track-bucket" key={b.riskLevel}>
                            <span className="plan-track-risk">{b.riskLevel}</span>
                            <span className="plan-track-figs">
                                {pct(b.hitRatePct)} hit · {signedPct(b.avgRealizedReturnPct)} · {b.count} scored
                            </span>
                        </div>
                    ))}
                </div>
            )}

            <p className="plan-track-note">
                <Target size={13} aria-hidden="true" />
                {t('track.note')}
            </p>
        </Card>
    )
}
