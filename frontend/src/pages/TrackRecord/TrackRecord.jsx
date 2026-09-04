import React, { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ShieldCheck, TrendingDown, Activity } from 'lucide-react'
import { useShadowTrackRecord } from '@/features/trackRecord/useShadowTrackRecord'
import { useTrackRecord } from '@/features/goals/useTrackRecord'
import { Card, StatTile, LoadingState, ErrorState, EmptyState } from '@/shared/ui'
import './TrackRecord.css'

const pct = (n) => `${Number(n) >= 0 ? '+' : ''}${(Number(n) * 100).toFixed(1)}%`
const plainPct = (n) => `${Number(n).toFixed(1)}%`
const money = (n) => `$${Number(n).toLocaleString(undefined, { maximumFractionDigits: 0 })}`

/**
 * Builds the NAV polyline. Y is scaled to the series' own min/max rather than to
 * zero: these curves move a few percent over months, and a zero-based axis would
 * flatten every one of them into the same straight line.
 */
const buildPath = (points, width, height) => {
    if (points.length < 2) return null
    const navs = points.map((p) => p.nav)
    const min = Math.min(...navs)
    const max = Math.max(...navs)
    const span = max - min || 1
    const dx = width / (points.length - 1)
    return points
        .map(
            (p, i) =>
                `${i === 0 ? 'M' : 'L'}${(i * dx).toFixed(2)},${(
                    height - ((p.nav - min) / span) * height
                ).toFixed(2)}`,
        )
        .join(' ')
}

const W = 640
const H = 160

const NavChart = ({ points }) => {
    const path = useMemo(() => buildPath(points, W, H), [points])

    if (!path) {
        return (
            <p className="tr-chart-empty">
                Not enough history to plot yet — the curve starts once the portfolio has two valued days.
            </p>
        )
    }

    const up = points[points.length - 1].nav >= points[0].nav
    const stroke = up ? 'var(--color-success, #34d399)' : 'var(--color-danger, #f87171)'
    const dx = W / (points.length - 1)

    return (
        <svg
            className="tr-chart"
            viewBox={`0 0 ${W} ${H}`}
            preserveAspectRatio="none"
            role="img"
            aria-label={`Model portfolio value from ${points[0].date} to ${points[points.length - 1].date}`}
        >
            <path d={path} fill="none" stroke={stroke} strokeWidth="2" vectorEffect="non-scaling-stroke" />
            {/* Rebalance days are marked because that is when costs were charged —
                the reader should be able to see what they paid for. */}
            {points.map((p, i) =>
                p.rebalanced ? (
                    <line
                        key={p.date}
                        className="tr-rebal"
                        x1={(i * dx).toFixed(2)}
                        x2={(i * dx).toFixed(2)}
                        y1="0"
                        y2={H}
                        vectorEffect="non-scaling-stroke"
                    />
                ) : null,
            )}
        </svg>
    )
}

const SeriesCard = ({ s }) => {
    const rebalances = s.series.filter((p) => p.rebalanced).length
    return (
        <Card className="tr-series">
            <div className="tr-series-head">
                <div>
                    <h3>{s.templateName}</h3>
                    <span className="tr-series-meta">
                        {s.riskBand} · {s.rebalanceCadence} rebalance · since {s.inceptionDate}
                    </span>
                </div>
                <span className={`tr-badge ${s.totalReturn >= 0 ? 'up' : 'down'}`}>{pct(s.totalReturn)}</span>
            </div>

            <NavChart points={s.series} />

            <div className="tr-tiles">
                <StatTile
                    label="Total return"
                    value={pct(s.totalReturn)}
                    valueColor={s.totalReturn >= 0 ? 'var(--color-success)' : 'var(--color-danger)'}
                />
                <StatTile label="Annualized" value={pct(s.annualizedReturn)} />
                <StatTile label="Max drawdown" value={pct(s.maxDrawdown)} valueColor="var(--color-danger)" />
                <StatTile label="Days live" value={s.days.toLocaleString()} />
            </div>

            <p className="tr-series-foot">
                Started at {money(s.notional)} · now {money(s.currentNav)} · {rebalances} rebalance
                {rebalances === 1 ? '' : 's'}, each charged 25 bps per side.
            </p>
        </Card>
    )
}

/**
 * The public trust surface (MVP_PLAN § 5 step 2). Two different track records sit
 * here deliberately, because they answer two different questions:
 *
 *   — Model portfolios: what a portfolio following each strategy actually did,
 *     costs simulated. The honest analogue of a performance chart.
 *   — Prediction accuracy: how often the daily signal was directionally right,
 *     against the 50% base rate the ranking target guarantees by construction.
 *
 * Neither is flattering by design, and both stay on screen when the numbers are
 * bad. The backend's disclaimer is rendered verbatim — it is FRA-safe wording,
 * not copy for the frontend to improve on.
 */
const TrackRecord = () => {
    const shadow = useShadowTrackRecord()
    const realized = useTrackRecord()
    const [tab, setTab] = useState('portfolios')

    const window90 =
        realized.data?.windows?.find((w) => w.windowDays === 90) ?? realized.data?.windows?.[0]

    return (
        <div className="tr-page">
            <header className="tr-hero">
                <span className="tr-eyebrow">
                    <ShieldCheck size={14} aria-hidden="true" /> Measured, not marketed
                </span>
                <h1>Track record</h1>
                <p>
                    Every number here is computed from what actually happened — nightly, automatically,
                    with no opportunity to re-run a bad month. Losses stay on the page.
                </p>
                <Link className="tr-method-link" to="/methodology">
                    How these numbers are computed →
                </Link>
            </header>

            <div className="tr-tabs" role="tablist">
                <button
                    type="button"
                    role="tab"
                    aria-selected={tab === 'portfolios'}
                    className={tab === 'portfolios' ? 'active' : ''}
                    onClick={() => setTab('portfolios')}
                >
                    <Activity size={14} aria-hidden="true" /> Model portfolios
                </button>
                <button
                    type="button"
                    role="tab"
                    aria-selected={tab === 'accuracy'}
                    className={tab === 'accuracy' ? 'active' : ''}
                    onClick={() => setTab('accuracy')}
                >
                    <TrendingDown size={14} aria-hidden="true" /> Prediction accuracy
                </button>
            </div>

            {tab === 'portfolios' && (
                <section className="tr-section">
                    {shadow.isLoading ? (
                        <LoadingState label="Loading the model portfolios…" />
                    ) : shadow.isError ? (
                        <ErrorState
                            message="Couldn't load the model-portfolio track record."
                            onRetry={() => shadow.refetch()}
                        />
                    ) : !shadow.data?.portfolios?.length ? (
                        <EmptyState
                            title="No model portfolios yet"
                            hint="Each strategy template starts a paper portfolio on its first nightly run. The curve begins the day after inception."
                        />
                    ) : (
                        <>
                            <p className="tr-disclaimer">{shadow.data.disclaimer}</p>
                            <div className="tr-series-grid">
                                {shadow.data.portfolios.map((s) => (
                                    <SeriesCard key={s.templateKey} s={s} />
                                ))}
                            </div>
                        </>
                    )}
                </section>
            )}

            {tab === 'accuracy' && (
                <section className="tr-section">
                    {realized.isLoading ? (
                        <LoadingState label="Loading realized outcomes…" />
                    ) : realized.isError ? (
                        <ErrorState
                            message="Couldn't load prediction outcomes."
                            onRetry={() => realized.refetch()}
                        />
                    ) : !window90 || window90.count === 0 ? (
                        <EmptyState
                            title="No matured predictions yet"
                            hint="Predictions are scored 30 days after they are made, so the first figures appear a month after the first published run."
                        />
                    ) : (
                        <>
                            <p className="tr-disclaimer">
                                Every published prediction is marked to market 30 days later and scored
                                against what the stock actually did. Nothing here is back-fitted, and no
                                run is excluded after the fact.
                            </p>

                            <Card className="tr-accuracy">
                                <div className="tr-tiles">
                                    <StatTile
                                        label={`Hit rate (${window90.windowDays}d)`}
                                        value={plainPct(window90.hitRatePct)}
                                        valueColor="var(--qw-amber)"
                                    />
                                    <StatTile
                                        label="Avg realized return"
                                        value={`${window90.avgRealizedReturnPct >= 0 ? '+' : ''}${window90.avgRealizedReturnPct.toFixed(2)}%`}
                                        valueColor={
                                            window90.avgRealizedReturnPct >= 0
                                                ? 'var(--color-success)'
                                                : 'var(--color-danger)'
                                        }
                                    />
                                    <StatTile label="Scored in window" value={window90.count.toLocaleString()} />
                                    <StatTile
                                        label="Scored all time"
                                        value={realized.data.totalScored.toLocaleString()}
                                    />
                                </div>

                                <p className="tr-baseline">
                                    The model ranks stocks against each other, so the target is built to have a
                                    50% base rate. A hit rate near 50% is the honest null result — the edge is
                                    the distance above it, and it is small.
                                </p>

                                {window90.byRiskLevel?.length > 0 && (
                                    <div className="tr-buckets">
                                        {window90.byRiskLevel.map((b) => (
                                            <div className="tr-bucket" key={b.riskLevel}>
                                                <span className={`tr-risk risk-${b.riskLevel.toLowerCase()}`}>
                                                    {b.riskLevel}
                                                </span>
                                                <span>{plainPct(b.hitRatePct)} hit</span>
                                                <span>
                                                    {b.avgRealizedReturnPct >= 0 ? '+' : ''}
                                                    {b.avgRealizedReturnPct.toFixed(2)}%
                                                </span>
                                                <span className="tr-bucket-n">{b.count} scored</span>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </Card>
                        </>
                    )}
                </section>
            )}
        </div>
    )
}

export default TrackRecord
