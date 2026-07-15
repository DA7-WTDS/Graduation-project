import { Link } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { SignalPill, ConvictionBar, Skeleton, ErrorState, EmptyState } from '@/shared/ui'
import { useActiveGoal } from '@/features/goals/useActiveGoal'
import { useRecommendations } from './useRecommendations'
import './RecommendationsPanel.css'

const fmtUSD = (n: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n)

/**
 * The product: live LLM recommendations rendered as dense, signal-coded rows.
 * Each row: [ticker · BUY/SELL/HOLD pill · allocation bar · alloc%] + reason / risk / fit.
 * The user's investment amount (from their goal) turns each allocation % into a
 * dollar figure.
 */
export function RecommendationsPanel() {
    const { data, isLoading, isError, error, refetch } = useRecommendations()
    const { investmentAmount } = useActiveGoal()
    const noRun = error instanceof ApiError && error.status === 404

    const investAmount = Math.max(0, investmentAmount)

    return (
        <div className="recs">
            <div className="recs-head">
                <span className="recs-title">AI Recommendations</span>
                <span className="recs-cadence" title="Regenerated automatically each day from the latest model run">
                    Updates daily
                </span>
            </div>

            {isLoading ? (
                <div className="recs-rows" aria-busy="true">
                    {Array.from({ length: 4 }).map((_, i) => (
                        <div className="recs-row" key={i}>
                            <div className="recs-line">
                                <Skeleton width={48} height={14} />
                                <Skeleton width={48} height={18} radius="4px" />
                                <Skeleton height={6} />
                                <Skeleton width={28} height={12} />
                            </div>
                            <Skeleton width="75%" height={10} />
                        </div>
                    ))}
                </div>
            ) : noRun ? (
                <EmptyState
                    title="No run yet"
                    hint="The daily pipeline hasn't produced a run for your profile. Check back later."
                />
            ) : isError ? (
                <ErrorState message="Recommendations are unavailable right now." onRetry={() => refetch()} />
            ) : !data || data.picks.length === 0 ? (
                <EmptyState title="No picks" hint="This run produced no picks that fit your profile." />
            ) : (
                <>
                    {data.summary && <p className="recs-summary">{data.summary}</p>}

                    {investAmount > 0 ? (
                        <div className="recs-invest">
                            <span className="recs-invest-label">Investing</span>
                            <span className="recs-invest-value">{fmtUSD(investAmount)}</span>
                            <Link to="/onboarding" className="recs-invest-edit">Change</Link>
                        </div>
                    ) : (
                        <div className="recs-invest recs-invest-empty">
                            <span>Set your investment amount to see dollar allocations.</span>
                            <Link to="/onboarding" className="recs-invest-edit">Set amount</Link>
                        </div>
                    )}

                    <div className="recs-rows">
                        {data.picks.map((pick, i) => {
                            const dollars = (investAmount * pick.allocation_pct) / 100
                            return (
                                <div className="recs-row" key={`${pick.ticker}-${i}`}>
                                    <div className="recs-line">
                                        <span className="recs-ticker">{pick.ticker}</span>
                                        <SignalPill action={pick.action} />
                                        <ConvictionBar value={pick.allocation_pct / 100} signal={pick.action} />
                                        <span className="recs-alloc">
                                            {investAmount > 0 && pick.allocation_pct > 0 && (
                                                <span className="recs-alloc-amount">{fmtUSD(dollars)}</span>
                                            )}
                                            <span className="recs-alloc-pct">
                                                {pick.allocation_pct > 0 ? `${Math.round(pick.allocation_pct)}%` : '—'}
                                            </span>
                                        </span>
                                    </div>
                                    <p className="recs-reason">{pick.reason}</p>
                                    {pick.risk_note && <p className="recs-risk">{pick.risk_note}</p>}
                                    {pick.fit && <p className="recs-fit">{pick.fit}</p>}
                                </div>
                            )
                        })}
                    </div>

                    {investAmount > 0 && (() => {
                        const totalPct = data.picks.reduce((sum, p) => sum + Math.max(0, p.allocation_pct), 0)
                        const allocated = (investAmount * totalPct) / 100
                        return (
                            <div className="recs-invest-total">
                                <span>Allocated across {data.picks.filter(p => p.allocation_pct > 0).length} picks</span>
                                <span className="recs-invest-total-val">
                                    {fmtUSD(allocated)}
                                    {totalPct < 100 && <span className="recs-invest-cash"> · {fmtUSD(investAmount - allocated)} cash</span>}
                                </span>
                            </div>
                        )
                    })()}

                    <p className="recs-foot">
                        Generated {new Date(data.generated_at).toLocaleString()} · informational only, not financial advice
                    </p>
                </>
            )}
        </div>
    )
}
