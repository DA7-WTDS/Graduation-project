import { usePortfolio } from '@/features/portfolio/usePortfolio'
import { LoadingState, EmptyState } from '@/shared/ui'
import { useRecommendations } from './useRecommendations'
import './TargetMix.css'

const fmtUSD = (n: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n)

// On-palette colors cycled across the held tickers.
const MIX_PALETTE = ['var(--qw-amber)', '#FFC23A', 'var(--qw-amber-dim)', '#C9892B', 'var(--qw-text-dim)', '#9C7A3C']

/**
 * "What the user actually invests in" — the recommended buys (allocation > 0)
 * as a single horizontal bar + legend, with dollar amounts from the user's
 * stored investment amount. Shared by the Dashboard and Portfolios pages.
 */
export function TargetMix() {
    const { data: portfolio } = usePortfolio()
    const recs = useRecommendations()

    const investAmount = Math.max(0, portfolio?.investmentAmount ?? 0)

    const holdings = (() => {
        const buys = (recs.data?.picks ?? [])
            .filter(p => p.allocation_pct > 0)
            .sort((a, b) => b.allocation_pct - a.allocation_pct)
        const list = buys.map((p, i) => ({ label: p.ticker, value: p.allocation_pct, color: MIX_PALETTE[i % MIX_PALETTE.length] }))
        const allocated = buys.reduce((s, p) => s + p.allocation_pct, 0)
        if (allocated < 99.5) list.push({ label: 'Cash', value: 100 - allocated, color: 'var(--qw-text-faint)' })
        return list
    })()

    return (
        <div className="qw-mix">
            <div className="qw-mix-head">
                <span className="qw-mix-label">Target Mix</span>
                <span className="qw-mix-sub">
                    {investAmount > 0
                        ? `Investing ${fmtUSD(investAmount)}`
                        : portfolio
                            ? `${portfolio.primaryGoal} · ${portfolio.timeHorizon}`
                            : ''}
                </span>
            </div>

            {recs.isLoading ? (
                <LoadingState label="Loading your mix…" />
            ) : holdings.length === 0 ? (
                <EmptyState
                    title="No positions yet"
                    hint="Your investment mix appears once the daily run produces picks."
                />
            ) : (
                <>
                    <div className="qw-mix-bar">
                        {holdings.map(a => (
                            <span key={a.label} style={{ width: `${a.value}%`, background: a.color }} />
                        ))}
                    </div>
                    <div className="qw-mix-legend">
                        {holdings.map(a => (
                            <span key={a.label} className="qw-mix-item">
                                <i style={{ background: a.color }} />{a.label} {Math.round(a.value)}%
                                {investAmount > 0 && (
                                    <em className="qw-mix-amt">{fmtUSD((investAmount * a.value) / 100)}</em>
                                )}
                            </span>
                        ))}
                    </div>
                </>
            )}
        </div>
    )
}
