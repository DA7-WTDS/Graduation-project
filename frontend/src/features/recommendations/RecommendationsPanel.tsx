import { ApiError } from '@/shared/api/client'
import { SignalPill, ConvictionBar, Skeleton, ErrorState, EmptyState } from '@/shared/ui'
import { useRecommendations } from './useRecommendations'
import './RecommendationsPanel.css'

/**
 * The product: live LLM recommendations rendered as dense, signal-coded rows.
 * Each row: [ticker · BUY/SELL/HOLD pill · allocation bar · alloc%] + reason / risk / fit.
 */
export function RecommendationsPanel() {
    const { data, isLoading, isError, error, refetch, isFetching } = useRecommendations()
    const noRun = error instanceof ApiError && error.status === 404

    return (
        <div className="recs">
            <div className="recs-head">
                <span className="recs-title">AI Recommendations</span>
                <button
                    type="button"
                    className="recs-refresh"
                    onClick={() => refetch()}
                    disabled={isFetching}
                >
                    {isFetching ? 'Refreshing…' : 'Refresh'}
                </button>
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

                    <div className="recs-rows">
                        {data.picks.map((pick, i) => (
                            <div className="recs-row" key={`${pick.ticker}-${i}`}>
                                <div className="recs-line">
                                    <span className="recs-ticker">{pick.ticker}</span>
                                    <SignalPill action={pick.action} />
                                    <ConvictionBar value={pick.allocation_pct / 100} signal={pick.action} />
                                    <span className="recs-alloc">
                                        {pick.allocation_pct > 0 ? `${Math.round(pick.allocation_pct)}%` : '—'}
                                    </span>
                                </div>
                                <p className="recs-reason">{pick.reason}</p>
                                {pick.risk_note && <p className="recs-risk">{pick.risk_note}</p>}
                                {pick.fit && <p className="recs-fit">{pick.fit}</p>}
                            </div>
                        ))}
                    </div>

                    <p className="recs-foot">
                        Generated {new Date(data.generated_at).toLocaleString()} · informational only, not financial advice
                    </p>
                </>
            )}
        </div>
    )
}
