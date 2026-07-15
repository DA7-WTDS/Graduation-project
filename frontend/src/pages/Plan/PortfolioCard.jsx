import React from 'react'
import { TrendingDown, Scale, CalendarClock } from 'lucide-react'
import { Card, StatTile } from '@/shared/ui'

const money = (n) => `$${Number(n).toLocaleString(undefined, { maximumFractionDigits: 0 })}`
const pct1 = (n) => `${(Number(n) * 100).toFixed(1)}%`
const signedPct = (n) => `${n >= 0 ? '+' : ''}${(Number(n) * 100).toFixed(1)}%`
const date = (s) => new Date(s).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })

const SLEEVE_COLORS = {
    core: 'var(--qw-amber)',
    tactical: 'var(--qw-amber-dim)',
    stability: 'var(--qw-text-dim)',
    speculative: 'var(--qw-text-faint)',
}

/**
 * The live view of the accepted portfolio: what it's worth now, how far it is
 * from its high-water mark, and how far each position has drifted from target.
 * Everything here is server-computed — the client only formats.
 */
export const PortfolioCard = ({ portfolio }) => {
    const positions = portfolio.positions

    return (
        <Card className="plan-live">
            <div className="plan-card-head">
                <span className="plan-label">Your portfolio</span>
                <span className="plan-live-valued">
                    {portfolio.pricesComplete
                        ? 'Live · registry closes'
                        : `Last valued ${portfolio.valuedAt ? date(portfolio.valuedAt) : 'not yet'}`}
                </span>
            </div>

            <div className="plan-tiles">
                <StatTile label="Value" value={money(portfolio.nav)} valueColor="var(--qw-amber)" />
                <StatTile
                    label="Total Return"
                    value={signedPct(portfolio.totalReturnPct)}
                    valueColor={portfolio.totalReturnPct >= 0 ? 'var(--color-success)' : 'var(--color-danger)'}
                />
                <StatTile label="From High" value={portfolio.drawdownPct > 0 ? `−${pct1(portfolio.drawdownPct)}` : 'At high'} />
                <StatTile label="Next Review" value={date(portfolio.nextReviewDate)} />
            </div>

            {portfolio.drawdownAlertActive && (
                <p className="plan-alert plan-alert-warn">
                    <TrendingDown size={14} aria-hidden="true" />
                    Down {pct1(portfolio.drawdownPct)} from its high — past this plan's {pct1(portfolio.drawdownThreshold)} alert level.
                </p>
            )}
            {portfolio.driftAlertActive && (
                <p className="plan-alert">
                    <Scale size={14} aria-hidden="true" />
                    Allocation has drifted from target — regenerate a proposal to rebalance.
                </p>
            )}

            <div className="plan-live-positions">
                <div className="plan-live-row plan-live-head-row">
                    <span />
                    <span>Symbol</span>
                    <span className="num">Target</span>
                    <span className="num">Actual</span>
                    <span className="num">Drift</span>
                    <span className="num">Value</span>
                </div>
                {positions.map((p) => (
                    <div className="plan-live-row" key={p.symbol}>
                        <span className="plan-pos-dot" style={{ background: SLEEVE_COLORS[p.sleeve] ?? 'var(--qw-text-faint)' }} />
                        <span className="plan-pos-sym">{p.symbol}</span>
                        <span className="num plan-live-target">{pct1(p.targetWeight)}</span>
                        <span className="num plan-live-actual">{p.actualWeight === null ? '—' : pct1(p.actualWeight)}</span>
                        <span className={`num plan-live-drift${driftClass(p.driftPct)}`}>
                            {p.driftPct === null ? '—' : signedPct(p.driftPct)}
                        </span>
                        <span className="num plan-live-value">{p.currentValue === null ? '—' : money(p.currentValue)}</span>
                    </div>
                ))}
            </div>

            <p className="plan-live-foot">
                <CalendarClock size={13} aria-hidden="true" />
                {portfolio.templateName} · started {date(portfolio.inceptionDate)} · rebalances{' '}
                {portfolio.rebalanceCadence.replace('_', '-')}
            </p>
        </Card>
    )
}

// Only flag drift once it's material enough to act on (matches the 10pp
// server-side rebalance threshold).
const driftClass = (drift) => {
    if (drift === null) return ''
    return Math.abs(drift) >= 0.1 ? ' wide' : ''
}
