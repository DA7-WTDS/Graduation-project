import React from 'react'
import { Target } from 'lucide-react'
import { Card, StatTile, EmptyState } from '@/shared/ui'

const pct = (n) => `${Number(n).toFixed(1)}%`
const signedPct = (n) => `${n >= 0 ? '+' : ''}${Number(n).toFixed(2)}%`

/**
 * Our realized track record (§ 0.3) — the honest answer to "how right have you
 * been?", shown to the user unedited. Predictions are scored against what the
 * market actually did 30 days later; nothing here is back-fitted, and a bad
 * number stays on screen.
 */
export const TrackRecordCard = ({ trackRecord }) => {
    const window = trackRecord.windows?.find((w) => w.windowDays === 90) ?? trackRecord.windows?.[0]

    if (!window || window.count === 0) {
        return (
            <Card className="plan-track">
                <div className="plan-card-head">
                    <span className="plan-label">Our track record</span>
                </div>
                <EmptyState
                    title="Not enough scored predictions yet"
                    hint="Predictions are scored once their 30-day horizon matures. This fills in as outcomes land."
                />
            </Card>
        )
    }

    return (
        <Card className="plan-track">
            <div className="plan-card-head">
                <span className="plan-label">Our track record</span>
                <span className="plan-track-window">last {window.windowDays} days · {window.count} scored</span>
            </div>

            <div className="plan-tiles">
                <StatTile label="Direction Hit Rate" value={pct(window.hitRatePct)} valueColor="var(--qw-amber)" />
                <StatTile
                    label="Avg Realized Return"
                    value={signedPct(window.avgRealizedReturnPct)}
                    valueColor={window.avgRealizedReturnPct >= 0 ? 'var(--color-success)' : 'var(--color-danger)'}
                />
                <StatTile label="Predictions Scored" value={window.count.toLocaleString()} />
                <StatTile label="All Time" value={trackRecord.totalScored.toLocaleString()} />
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
                Every prediction is scored against what the market actually did 30 days later — wins and losses alike.
                Past results never guarantee future ones. Informational only, not financial advice.
            </p>
        </Card>
    )
}
